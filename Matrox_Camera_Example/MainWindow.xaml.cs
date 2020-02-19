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
        Device.MatroxCLCamera dev;
        public List<Device.CrevisImage> Test;
        public MainWindow()
        {
            InitializeComponent();

            Test = new List<Device.CrevisImage>();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dev = new Device.MatroxCLCamera();
            var res = dev.Open();
            res = dev.AcqStart();
            ThreadPool.QueueUserWorkItem(Continuous_Grab);
        }

        private void Continuous_Grab(object status)
        {
            for (int i = 0; i < 10; i++)
            {
                var res = dev.Grab(Device.ETriggerOption.Continuous);
                Dispatcher.Invoke(new Action(() =>
                {
                    ImageCon.Source = dev.CameraList[0].CrevisImage.BitmapSourceImage;
                }), System.Windows.Threading.DispatcherPriority.DataBind);
                lock (aaa)
                {
                    Test.Add(dev.CameraList[0].CrevisImage);
                }

            }
            lock (aaa)
            {
                foreach (var bmp in Test)
                {
                    var str = AppDomain.CurrentDomain.BaseDirectory + DateTime.Now.ToString("HHmmss_ffff") + ".bmp";
                    bmp.BitmapImage.Save(str, System.Drawing.Imaging.ImageFormat.Bmp);
                    bmp.Dispose();
                }
                Test.Clear();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(Continuous_Grab);
        }
        object aaa = new object();
        
    }
}
