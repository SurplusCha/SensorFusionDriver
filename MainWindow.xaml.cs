using OpenCvSharp;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HelixToolkit.Wpf;
using static System.Net.WebRequestMethods;
using Microsoft.Win32;

namespace SensorFusionDriver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window, CameraDriverListener, LiDARDriverListener
    {
        private RealSenseDriver CameraDriver;
        private PuckDriver LiDARDriver;
        public MainWindow()
        {
            InitializeComponent();
            CameraDriver = new RealSenseDriver();
            LiDARDriver = new PuckDriver();
            CameraDriver.AddListener(this);
            LiDARDriver.AddListener(this);
            realsense_stop.IsEnabled = false;
            puck_stop.IsEnabled = false;
            recording_start.IsEnabled = true;
            recording_stop.IsEnabled = false;
            camera_decoding_enable.IsChecked = true;
            depth_decoding_enable.IsChecked = false;
            camera_display_enable.IsChecked = false;
            lidar_decoding_enable.IsChecked = true;
            lidar_display_enable.IsChecked = false;

            /*
            PerspectiveCamera myCamera = new PerspectiveCamera();
            myCamera.Position = new Point3D(0, 0, 0);
            myCamera.LookDirection = new Vector3D(0, 0, -1);
            pointcloud_view.Camera = myCamera;
            */
            pointcloud_view.RotateGesture = new MouseGesture(MouseAction.LeftClick);
            // var lights = new DefaultLights();
            // pointcloud_view.Children.Add(lights);
            // var teaPot = new SphereVisual3D();
            // pointcloud_view.Children.Add(teaPot);
        }
        public bool OnCameraImage(Mat img)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                camera_image.Source = OpenCvSharp.WpfExtensions.WriteableBitmapConverter.ToWriteableBitmap(img);
            }));
            return true;
        }
        public bool OnCameraDepth(Mat img)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                camera_depth.Source = OpenCvSharp.WpfExtensions.WriteableBitmapConverter.ToWriteableBitmap(img);
            }));
            return true;
        }
        public bool OnCameraLog(string log)
        {
            return true;
        }
        public bool OnCameraFPS(float fps)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                camera_fps.Content = fps.ToString("F1") + " fps";
            }));
            return true;
        }

        public bool OnCameraStarted()
        {
            Dispatcher.BeginInvoke(new Action(() => {
                realsense_stop.IsEnabled = true;
            }));

            return true;
        }

        public bool OnCameraStopped()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                realsense_start.IsEnabled = true;
                realsense_stop.IsEnabled = false;
            }));
            return true;
        }

        public bool OnCameraError(string error)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }));
            return true;
        }
        public bool OnLiDARStarted()
        {
            Dispatcher.BeginInvoke(new Action(() => {
                puck_stop.IsEnabled = true;
            }));
            return true;
        }

        public bool OnLiDARPointCloud(List<LiDARPoint3D> pointCloud)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                pointcloud_view.Children.Clear();
                var pts = new Point3DCollection();
                foreach (var pt in pointCloud)
                {
                    pts.Add(new Point3D(pt.X, pt.Y, pt.Z));
                }
                var cloudPoints = new PointsVisual3D { Color = Colors.Red, Size = 2 };
                cloudPoints.Points = pts;
                pointcloud_view.Children.Add(cloudPoints);
            }));
            return true;
        }

        public bool OnLiDARStopped()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                puck_start.IsEnabled = true;
                puck_stop.IsEnabled = false;
            }));
            return true;
        }
        public bool OnLiDARFPS(float fps)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                puck_fps.Content = fps.ToString("F1") + " fps";
            }));
            return true;
        }

        private async void RealSense_Start_Button_Click(object sender, RoutedEventArgs e)
        {
            realsense_start.IsEnabled = false;
            await Task.Run(() =>
            {
                CameraDriver.Start();
            });
        }

        private async void RealSense_Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                CameraDriver.Stop();
            });
        }

        private void Recording_file_path_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private async void camera_enable_Checked(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                CameraDriver.OnCameraDecoding();
            });
        }

        private async void depth_enable_Checked(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                CameraDriver.OnDepthDecoding();
            });
        }

        private async void camera_enable_Unchecked(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                CameraDriver.OffCameraDecoding();
            });
        }

        private async void depth_enable_Unchecked(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                CameraDriver.OffDepthDecoding();
            });
        }

        private async void LiDAR_Start_Button_Click(object sender, RoutedEventArgs e)
        {
            puck_start.IsEnabled = false;
            var portNum = Int32.Parse(puck_port_box.Text);
            await Task.Run(() =>
            {
                LiDARDriver.Start(portNum);
            });
        }

        private async void LiDAR_Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                LiDARDriver.Stop();
            });
        }

        private void Recording_Start_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.Multiselect = false;
            
            if (openFolderDialog.ShowDialog() == true)
            {
                recording_stop.IsEnabled = true;
                recording_start.IsEnabled = false;
                var folderName = openFolderDialog.FolderName;
                recording_folder_path.Content = folderName;

                CameraDriver.OnCameraWriting(folderName);
                LiDARDriver.OnLiDARWriting(folderName);
            }
        }

        private void Recording_Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            CameraDriver.OffCameraWriting();
            LiDARDriver.OffLiDARWriting();
            recording_stop.IsEnabled = false;
            recording_start.IsEnabled = true;
        }

        private async void camera_display_enable_Checked(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                CameraDriver.OnCameraDisplay();
            });
        }
        private async void camera_display_enable_Unchecked(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                CameraDriver.OffCameraDisplay();
            });
        }

        private async void lidar_enable_Checked(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                LiDARDriver.OnLiDARDecoding();
            });
        }
        private async void lidar_enable_Unchecked(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                LiDARDriver.OffLiDARDecoding();
            });
        }

        private async void lidar_display_enable_Checked(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                LiDARDriver.OnLiDARDisplay();
            });
        }
        private async void lidar_display_enable_Unchecked(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                LiDARDriver.OffLiDARDisplay();
            });
        }
    }
}