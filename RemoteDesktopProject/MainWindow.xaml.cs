using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RemoteDesktopProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpClient? _client;
        private bool _isReceiving = false;
        private Thread? receiverThread;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Share_Click(object sender, RoutedEventArgs e)
        {
            if (_isReceiving)
            {
                _isReceiving = false;
                receiverThread!.Join();
                _client?.Close();
                button_Share.Content = "Share";
            }
            else
            {
                _isReceiving = true;
                receiverThread = new Thread(ReceiveScreen);
                receiverThread.Start();
                button_Share.Content = "Stop";
            }
        }

        private void ReceiveScreen()
        {
            try
            {
                _client = new TcpClient("192.168.201.10", 8888);
                NetworkStream stream = _client.GetStream();

                using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    textBox_DeviceName.Text = reader.ReadString();
                }

                while (_isReceiving)
                {
                    // Read the size of the incoming image
                    byte[] sizeBuffer = new byte[4];
                    stream.Read(sizeBuffer, 0, 4);
                    int imageSize = BitConverter.ToInt32(sizeBuffer, 0);

                    // Read the image data
                    byte[] imageBuffer = new byte[imageSize];
                    int bytesRead = 0;
                    while (bytesRead < imageSize)
                    {
                        bytesRead += stream.Read(imageBuffer, bytesRead, imageSize - bytesRead);
                    }

                    // Display the image
                    using (MemoryStream ms = new MemoryStream(imageBuffer))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        Dispatcher.Invoke(() =>
                        {
                            image_Screen.Source = bitmap;
                        });
                    }
                }

                stream.Close();
                _client.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}