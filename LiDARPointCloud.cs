using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorFusionDriver
{
    internal class LiDARPointCloud
    {
        public float Azimuth;
        public List<LiDARPoint3D> Points;

        public LiDARPointCloud(float azimuth, List<LiDARPoint3D> points)
        {
            Azimuth = azimuth;
            Points = points;
        }
    }
}
