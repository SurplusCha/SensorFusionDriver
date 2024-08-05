using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorFusionDriver
{
    public class LiDARPoint3D
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float Reflect { get; }
        public LiDARPoint3D(float x, float y, float z, float reflect)
        {
            X = x;
            Y = y;
            Z = z;
            Reflect = reflect;
        }
    }
}
