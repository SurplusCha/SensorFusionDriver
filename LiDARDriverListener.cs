using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorFusionDriver
{
    internal interface LiDARDriverListener
    {
        bool OnLiDARStarted();
        bool OnLiDARPointCloud(List<LiDARPoint3D> pointCloud);
        bool OnLiDARStopped();
        bool OnLiDARFPS(float fps);
    }
}
