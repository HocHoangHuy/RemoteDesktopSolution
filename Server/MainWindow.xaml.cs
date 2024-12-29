using Client;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient server = null, mouse = null;

        int FrameCount = 0;
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => { StartReceiving(); };

            //this.Loaded += (s, e) => { captureScreen = StartCaptureScreen(); };
            //ScreenStateLogger screenStateLogger = new();
            //screenStateLogger.ScreenRefreshed += (s, e) => { image_Screen.Dispatcher.Invoke(() => ChangeSource(e)); };
            //screenStateLogger.Start();
        }

        private Task StartReceiving()
        {
            Task screenCapturing = new Task(() =>
            {
                Thread.CurrentThread.Name = "Receiving images";
                server = new TcpClient();
                server.Connect("192.168.112.111", 8888);
                NetworkStream networkStream = server.GetStream();
                while (Thread.CurrentThread.IsAlive || server.Connected)
                {
                    byte[] byteLengthBuffer = new byte[4];
                    networkStream.ReadExactly(byteLengthBuffer, 0, 4);
                    int ByteLength = BitConverter.ToInt32(byteLengthBuffer);
                    byte[] buffer = new byte[ByteLength];
                    networkStream.ReadExactly(buffer, 0, ByteLength);
                    image_Screen.Dispatcher.Invoke(() =>
                    {
                        ChangeSource(buffer);
                    }, System.Windows.Threading.DispatcherPriority.Normal);
                    //Thread.Sleep(1000 / 30);
                }
            });
            screenCapturing.Start();
            mouse = new("192.168.112.111", 8889);
            return screenCapturing;

        }

        private Thread StartCaptureScreen()
        {
            Thread captureScreen = new Thread(() =>
            {
                {
                    ScreenCapture screenCapturer = new();
                    //ScreenStateLogger screenStateLogger = new();
                    MemoryStream memoryStream;

                    while (Thread.CurrentThread.IsAlive)
                    {
                        Bitmap screenBitmap = screenCapturer.GetCapturedFrame();
                        if (screenBitmap == null) continue;
                        memoryStream = new MemoryStream();
                        screenBitmap.Save(memoryStream, ImageFormat.Jpeg);
                        image_Screen.Dispatcher.Invoke(() =>
                        {
                            ChangeSource(memoryStream);
                        }, System.Windows.Threading.DispatcherPriority.Normal);
                    }
                }
            });
            captureScreen.Name = "CaptureScreenThread";
            captureScreen.Start();
            return captureScreen;
        }

        private void ChangeSource(MemoryStream memoryStream)
        {
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
            //bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            image_Screen.Source = bitmapImage;
        }
        private void ChangeSource(byte[] data)
        {
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(data);
            //bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            image_Screen.Source = bitmapImage;
        }

        private void image_Screen_MouseMove(object sender, MouseEventArgs e)
        {
            System.Windows.Point mousePosition = e.GetPosition(image_Screen);
            mousePosition.X /= image_Screen.ActualWidth;
            mousePosition.Y /= image_Screen.ActualHeight;

        }

        private void image_Screen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point clickPoint = e.GetPosition(image_Screen);
            NetworkStream ns = mouse.GetStream();
            ns.WriteByte(1);
            ns.Write(BitConverter.GetBytes(clickPoint.X/image_Screen.ActualWidth));
            ns.Write(BitConverter.GetBytes(clickPoint.Y/image_Screen.ActualHeight));
        }

    }
}
