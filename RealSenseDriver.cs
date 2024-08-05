using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intel.RealSense;
using System.Threading.Tasks;
using OpenCvSharp;
using System.Windows.Media;
using OpenCvSharp.Aruco;
using System.IO;

namespace SensorFusionDriver
{
    internal class RealSenseDriver
    {
        private List<CameraDriverListener> listeners = new();
        private Intel.RealSense.Pipeline pipeline;
        private volatile bool SetRun = false;
        private volatile bool SetCameraDisplay = false;
        private volatile bool SetCameraDecoding = true;
        private volatile bool SetDepthDecoding = false;
        private volatile bool SetWriting = false;
        private string FilePath = "";
        private int FrameNum = 0;
        private StreamWriter? TimeStampStream;

        public RealSenseDriver() 
        {
            pipeline = new Pipeline();
        }

        ~RealSenseDriver()
        {
            TimeStampStream?.Dispose();
        }


        public bool AddListener(CameraDriverListener listener)
        {
            listeners.Add(listener);
            return true;
        }
        public bool OnCameraDecoding()
        {
            SetCameraDecoding = true;
            return true;
        }
        public bool OffCameraDecoding()
        {
            SetCameraDecoding = false;
            return true;
        }
        public bool OnDepthDecoding()
        {
            SetDepthDecoding = true;
            return true;
        }
        public bool OffDepthDecoding()
        {
            SetDepthDecoding = false;
            return true;
        }
        public bool OnCameraDisplay()
        {
            SetCameraDisplay = true;
            return true;
        }
        public bool OffCameraDisplay()
        {
            SetCameraDisplay = false;
            return true;
        }
        public bool OnCameraWriting(string path)
        {
            SetWriting = true;
            FilePath = path;

            if (!System.IO.Directory.Exists(Path.Combine(FilePath, "image_00")))
            {
                System.IO.Directory.CreateDirectory(Path.Combine(FilePath, "image_00"));
                System.IO.Directory.CreateDirectory(Path.Combine(FilePath, "image_00", "data_rect"));
                System.IO.File.Create(Path.Combine(FilePath, "image_00", "timestamps.txt"));
            }
            else if (!System.IO.Directory.Exists(Path.Combine(FilePath, "image_00", "data")))
            {
                System.IO.Directory.CreateDirectory(Path.Combine(FilePath, "image_00", "data_rect"));
                System.IO.File.Create(Path.Combine(FilePath, "image_00", "timestamps.txt"));
            }

            TimeStampStream = new StreamWriter(Path.Combine(FilePath, 
                "image_00", "timestamps.txt"));
            return true;
        }
        public bool OffCameraWriting()
        {
            SetWriting = false;
            return true;
        }
        public bool Start()
        {
            var config = new Intel.RealSense.Config();
            config.EnableStream(Intel.RealSense.Stream.Color, Intel.RealSense.Format.Bgr8);
            config.EnableStream(Intel.RealSense.Stream.Depth, Intel.RealSense.Format.Z16);

            try
            {
                var profile = pipeline.Start(config);
                var device = profile.Device;
                bool setRGB = false;
                foreach (var sensor in device.Sensors)
                {
                    if (sensor.Info.GetInfo(Intel.RealSense.CameraInfo.Name) == "RGB Camera")
                        setRGB = true;
                }
                if (!setRGB)
                {
                    foreach (var listener in listeners)
                        listener.OnCameraError("RGB Camera is not found!");

                    return false;
                }
            }
            catch (Exception e)
            {
                foreach (var listener in listeners)
                {
                    listener.OnCameraError(e.Message);
                    listener.OnCameraStopped();
                }
                return false;
            }
                      
            SetRun = true;
            Task.Run(() =>
            {
                Run();
            });

            foreach (var listener in listeners)
            {
                listener.OnCameraStarted();
            }

            return true;
        }

        public bool Stop()
        {
            SetRun = false;
            TimeStampStream?.Dispose();
            return true;
        }

        private bool Run()
        {
            var startTime = DateTime.Now;
            while (SetRun)
            {
                using (var frames = pipeline.WaitForFrames())
                {
                    ++FrameNum;
                    if (SetCameraDecoding)
                    {
                        using (var colors = frames.ColorFrame)
                        {
                            Mat colorImg = new Mat(colors.Height, colors.Width, MatType.CV_8UC3, colors.Data);
                            if (SetCameraDisplay)
                                foreach (var listener in listeners)
                                    listener.OnCameraImage(colorImg);

                            if (SetWriting)
                            {
                                Task.Run(() =>
                                {
                                    var fileName = FrameNum.ToString("D10") + ".png";
                                    Cv2.ImWrite(Path.Combine(FilePath, "image_00", "data_rect", fileName), colorImg);
                                });
                            }
                        }
                    }
                    if (SetDepthDecoding)
                    {
                        using (var depth = frames.DepthFrame)
                        {
                            // 16UC_1
                            Mat depthImg = new Mat(depth.Height, depth.Width, MatType.CV_16UC1, depth.Data);
                            if (SetCameraDisplay)
                            {
                                foreach (var listener in listeners)
                                    listener.OnCameraDepth(depthImg);
                            }

                            if (SetWriting)
                            {
                                // KITTI-360 doesn't need to depth image from camera
                                /*
                                Task.Run(() =>
                                {
                                    var fileName = frameNum.ToString("D10") + ".png";
                                    Cv2.ImWrite(Path.Combine(FilePath, "image_00", "data", fileName), depthImg);
                                });
                                */
                            }
                        }
                    }
                }

                var endTime = DateTime.Now;
                var timeStampForClient = endTime.ToString("yyyy-MM-dd HH:mm:ss.FFF");
                
                if (SetWriting)
                    TimeStampStream?.WriteLineAsync(timeStampForClient);

                var timeSpan = new TimeSpan(endTime.Ticks - startTime.Ticks);
                float fps = (float)FrameNum / (float)timeSpan.TotalSeconds;

                foreach (var listener in listeners)
                    listener.OnCameraFPS(fps);
            }

            pipeline.Stop();
            FrameNum = 0;
            foreach (var listener in listeners)
                listener.OnCameraStopped();

            return true;
        }
    }
}
