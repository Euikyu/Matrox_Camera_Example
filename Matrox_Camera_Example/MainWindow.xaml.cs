using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Matrox_Camera_Example
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        Device.MatroxCLCamera cam;
        public List<Device.CrevisImage> Test;
        public MainWindow()
        {
            InitializeComponent();

            Test = new List<Device.CrevisImage>();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cam = new Device.MatroxCLCamera();
            var res = cam.Open();
            res = cam.AcqStart();
            ThreadPool.QueueUserWorkItem(Continuous_Grab);
        }

        private void Continuous_Grab(object status)
        {
            while (true)
            {
                for (int i = 0; i < 1; i++)
                {
                    DateTime grabStart = DateTime.Now;
                    var res = cam.Grab(Device.ETriggerOption.Continuous);
                    if (res.ErrCode != Err.ErrProcess.ERR_SUCCESS) MessageBox.Show("Grab error.");
                    DateTime grabStop = DateTime.Now;
                    Dispatcher.Invoke(new Action(() =>
                    {
                        GrabTime.Text = (grabStop - grabStart).TotalMilliseconds.ToString("0.00");
                        DateTime dispStart = DateTime.Now;
                        ImageCon.Source = cam.CameraList[0].CrevisImage.BitmapSourceImage;
                        DisplayTime.Text = (DateTime.Now - dispStart).TotalMilliseconds.ToString("0.00");

                    }), System.Windows.Threading.DispatcherPriority.DataBind);
                    //lock (aaa)
                    //{
                    //    Test.Add(dev.CameraList[0].CrevisImage);
                    //}

                }
                //lock (aaa)
                //{
                //    foreach (var bmp in Test)
                //    {
                //        var str = AppDomain.CurrentDomain.BaseDirectory + DateTime.Now.ToString("HHmmss_ffff") + ".bmp";
                //        //bmp.BitmapImage.Save(str, System.Drawing.Imaging.ImageFormat.Bmp);
                //        bmp.Dispose();
                //    }
                //    Test.Clear();
                //}
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            cam.CameraList.OfType<Device.MatroxCLCamDevice>().First().SoftwareTrigger();
        }
        object aaa = new object();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cam.Dispose();
        }
    }
}
