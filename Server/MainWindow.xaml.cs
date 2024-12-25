using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
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
        Thread captureScreen;
        public MainWindow()
        {
            InitializeComponent();
            captureScreen = StartReceiving();
        }

        private Thread StartReceiving()
        {
            Thread screenCapturing = new Thread(() =>
            {
                TcpClient server = new TcpClient("192.168.201.10", 8888);
                NetworkStream networkStream = server.GetStream();
                while (Thread.CurrentThread.IsAlive || server.Connected)
                {
                    byte[] lengthBytes = new byte[4];
                    networkStream.Read(lengthBytes, 0, 4);
                    int ByteLength = BitConverter.ToInt32(lengthBytes);
                    byte[] buffer = new byte[ByteLength];
                    networkStream.Read(buffer, 0, ByteLength);
                    image_Screen.Dispatcher.Invoke(() =>
                    {
                        ChangeSource(buffer);
                    }, System.Windows.Threading.DispatcherPriority.Normal);
                }
            });
            screenCapturing.Name = "Receiving images";
            screenCapturing.Start();
            return screenCapturing;

        }

            private Thread StartCaptureScreen()
        {
            Thread captureScreen = new Thread(() =>
            {
                {
                    ScreenCapturer screenCapturer = new();
                    MemoryStream memoryStream;
                    memoryStream = new MemoryStream();
                    
                    while (Thread.CurrentThread.IsAlive)
                    {
                        Bitmap screenBitmap = screenCapturer.CaptureFrame();
                        memoryStream.Position = 0;
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
    }
}
