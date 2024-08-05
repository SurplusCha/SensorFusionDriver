using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorFusionDriver
{
    internal interface CameraDriverListener
    {
        bool OnCameraStarted();
        bool OnCameraImage(Mat img);
        bool OnCameraDepth(Mat img);
        bool OnCameraLog(string log);
        bool OnCameraFPS(float fps);
        bool OnCameraStopped();
        bool OnCameraError(string error);
    }
}
