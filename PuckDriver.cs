using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SensorFusionDriver
{
    internal class PuckDriver
    {
        private static readonly int PitchSize = 16;
        private List<LiDARDriverListener> listeners = new();
        private volatile bool SetStart = false;
        private volatile bool SetDecoding = true;
        private volatile bool SetDisplay = false;
        private volatile bool SetWriting = false;
        private int FrameNum = 0;
        private static readonly int TotalBlockNumber = 12;
        private static readonly int BlockSize = 100;
        private static readonly float[] LaserPitch = [-15, 1, -13, 3, -11, 5, -9, 7, -7, 9, -5, 11, -3, 13, -1, 15];
        private static readonly float[] LaserPitchCosine = new float[PitchSize];
        private static readonly float[] LaserPitchSine = new float[PitchSize];
        private string FilePath = "";
        private StreamWriter? TimeStampStream;

        public PuckDriver()
        {
            for (var i = 0; i < PitchSize; ++i)
            {
                var radian = Math.PI / 180 * LaserPitch[i];
                LaserPitchCosine[i] = (float)Math.Cos(radian);
                LaserPitchSine[i] = (float)Math.Sin(radian);
            }
        }

        ~PuckDriver()
        {
            TimeStampStream?.Dispose();
        }

        public bool AddListener(LiDARDriverListener listener)
        {
            listeners.Add(listener);
            return true;
        }

        public bool OnLiDARDecoding()
        {
            SetDecoding = true;
            return true;
        }
        public bool OffLiDARDecoding()
        {
            SetDecoding = false;
            return true;
        }
        public bool OnLiDARDisplay()
        {
            SetDisplay = true;
            return true;
        }
        public bool OffLiDARDisplay()
        {
            SetDisplay = false;
            return true;
        }
        public bool OnLiDARWriting(string path)
        {
            SetWriting = true;
            FilePath = path;
            
            if (!System.IO.Directory.Exists(Path.Combine(FilePath, "velodyne_points")))
            {
                System.IO.Directory.CreateDirectory(Path.Combine(FilePath, "velodyne_points"));
                System.IO.Directory.CreateDirectory(Path.Combine(FilePath, "velodyne_points", "data"));
                System.IO.File.Create(Path.Combine(FilePath, "velodyne_points", "timestamps.txt")).Close();
            }
            else if (!System.IO.Directory.Exists(Path.Combine(FilePath, "velodyne_points", "data")))
            {
                System.IO.Directory.CreateDirectory(Path.Combine(FilePath, "velodyne_points", "data"));
                System.IO.File.Create(Path.Combine(FilePath, "velodyne_points", "timestamps.txt")).Close();
            }

            TimeStampStream = new StreamWriter(Path.Combine(FilePath, "velodyne_points", "timestamps.txt"));
            return true;
        }
        public bool OffLiDARWriting()
        {
            SetWriting = false;
            TimeStampStream?.Dispose();
            return true;
        }

        public void Start(int Port)
        {
            SetStart = true;
            Task.Run(async() =>
            {
                using var udpClient = new UdpClient(Port);
                var startTime = DateTime.Now;
                float lastAzimuth = 0.0f;
                List<LiDARPoint3D> resultPts = new();

                while (SetStart)
                {
                    ++FrameNum;
                    if (SetDecoding)
                    {
                        var pointCloud = await udpClient.ReceiveAsync()
                        .ContinueWith(received => Parse(received.Result.Buffer))
                        .ContinueWith(packet => ConvertPointCloudFromPacket(packet.Result));

                        if (pointCloud.Azimuth < lastAzimuth)
                        {
                            var endTime = DateTime.Now;
                            var timeStampForClient = endTime.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                            var timeSpan = new TimeSpan(endTime.Ticks - startTime.Ticks);
                            float fps = (float)FrameNum / (float)timeSpan.TotalSeconds;

                            foreach (var listener in listeners)
                                listener.OnLiDARFPS(fps);

                            if (SetWriting)
                            {
                                var fileName = FrameNum.ToString("D10") + ".bin";
                                // TODO: Create a file and write some data
                                TimeStampStream?.WriteLineAsync(timeStampForClient);
                                using var fileStream = File.Create(Path.Combine(FilePath, "velodyne_points", "data", fileName));
                                using var binaryStream = new BinaryWriter(fileStream);
                                foreach (var pt in resultPts)
                                {
                                    binaryStream.Write(pt.X);
                                    binaryStream.Write(pt.Y);
                                    binaryStream.Write(pt.Z);
                                    binaryStream.Write(pt.Reflect);
                                }
                            }

                            if (SetDisplay)
                                foreach (var listener in listeners)
                                    listener.OnLiDARPointCloud(resultPts);

                            resultPts.Clear();
                        }
                        else
                            resultPts.AddRange(pointCloud.Points);

                        lastAzimuth = pointCloud.Azimuth;
                    }
                }


                foreach (var listener in listeners)
                    listener.OnLiDARStopped();
            });            
       
            foreach (var listener in listeners)
                listener.OnLiDARStarted();
        }

        public bool Stop()
        {
            SetStart = false;
            return true;
        }

        private PuckPacket Parse(byte[] buffer)
        {
            List<PuckPacket.DataBlock> dataBlocks = new();
            for (var i = 0; i < TotalBlockNumber; ++i)
            {
                // Validation Check
                if (buffer[i * BlockSize] == 0xff && buffer[i * BlockSize + 1] == 0xee)
                {
                    var azimuth = (float)(buffer[i * BlockSize + 3] * 256 
                        + buffer[i * BlockSize + 2]) / 100;
                    List<PuckPacket.DataPoint> dataPoints = new();
                    for (var j = 4; j < BlockSize; j = j + 3)
                    {
                        var dist = (float)(buffer[i * BlockSize + j + 1] * 256 
                            + buffer[i * BlockSize + j]);
                        dist = dist / 500;
                        var reflect = (float)buffer[i * BlockSize + j + 2];
                        dataPoints.Add(new(dist, reflect));
                    }
        
                    dataBlocks.Add(new(azimuth * (float)(Math.PI / 180), dataPoints));
                }
                else
                {
                    dataBlocks.Add(new(0, new List<PuckPacket.DataPoint>()));
                }
            }
            var timeStamp = (float)((long)buffer[buffer.Length - 6] * 16777216 
                + (long)buffer[buffer.Length - 5] * 65536 + (long)buffer[buffer.Length - 4] * 256 
                + (long)buffer[buffer.Length - 3]) / 1000000;
            return new PuckPacket(dataBlocks, timeStamp);
        }

        private LiDARPointCloud ConvertPointCloudFromPacket(PuckPacket packet)
        {
            List<LiDARPoint3D> pts = new();
            foreach (var dataBlock in packet.DataBlocks)
            {
                var azimuth = dataBlock.Azimuth;
                for (var i = 0; i < LaserPitch.Length; ++i)
                {
                    var distance = dataBlock.DataPoints[i].Distance;
                    if (distance != 0)
                    {
                        var x = (float)(distance * LaserPitchCosine[i]
                            * Math.Sin(azimuth));
                        var y = (float)(distance * LaserPitchCosine[i]
                            * Math.Cos(azimuth));
                        var z = (float)(distance * LaserPitchSine[i]);
                        pts.Add(new LiDARPoint3D(x, y, z, dataBlock.DataPoints[i].Reflecity));
                    }
                }
            }

            return new LiDARPointCloud(packet.DataBlocks[0].Azimuth, pts);
        }
    }
}
