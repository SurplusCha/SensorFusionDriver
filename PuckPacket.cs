using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorFusionDriver
{
    internal class PuckPacket
    {
        internal class DataPoint
        {
            public float Distance { get; } = 0.0f;
            public float Reflecity { get; } = 0.0f;
            public DataPoint(float distance, float reflecity)
            {
                Distance = distance;
                Reflecity = reflecity;
            }
        }

        internal class DataBlock
        {
            public float Azimuth { get; } = 0.0f;
            public List<DataPoint> DataPoints { get; }
            public DataBlock(float azimuth, List<DataPoint> dataPoints)
            {
                Azimuth = azimuth;
                DataPoints = dataPoints;
            }
        }

        public List<DataBlock> DataBlocks { get; }
        public float TimeStamp { get; }

        public PuckPacket(List<DataBlock> dataBlocks, float timeStamp)
        {
            DataBlocks = dataBlocks;
            TimeStamp = timeStamp;
        }
    }
}
