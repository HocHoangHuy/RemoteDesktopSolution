using System.Drawing.Imaging;
using System.Net.Sockets;
using System.Text;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private Thread? _streamThread = null;
        private bool _isStreaming = false;
        private Bitmap? _previousFrame = null;
        public Form1()
        {
            InitializeComponent();
            button_Connect.Text = "Share";
        }

        private void button_Connect_Click(object sender, EventArgs e)
        {
            if (_isStreaming)
            {
                _isStreaming = false;
                _streamThread?.Join();
                _previousFrame?.Dispose();
                button_Connect.Text = "Share";
            }
            else
            {
                _isStreaming = true;
                _streamThread = new Thread(StreamScreen);
                _streamThread.Start();
                button_Connect.Text = "Stop";
            }
        }

        private void StreamScreen()
        {
            try
            {
                TcpListener listener = new TcpListener(System.Net.IPAddress.Any, 8888);
                listener.Start();
                this.Invoke(() => { Enabled = false; });
                TcpClient client = listener.AcceptTcpClient();
                this.Invoke(() => { Enabled = true; });
                NetworkStream stream = client.GetStream();

                using(BinaryWriter writer = new(stream, Encoding.UTF8, true))
                {
                    writer.Write(Environment.MachineName);
                }

                while (_isStreaming)
                {
                    Bitmap currentFrame = CaptureScreen();
                    if (_previousFrame == null)
                    {
                        _previousFrame = new Bitmap(currentFrame);
                        SendFullFrame(currentFrame, stream);
                    }
                    else
                    {
                        SendDirtyRegions(currentFrame, _previousFrame, stream);
                        _previousFrame.Dispose();
                        _previousFrame = new Bitmap(currentFrame);
                    }

                    currentFrame.Dispose();
                    Thread.Sleep(50); // Limit frame rate
                }

                stream.Close();
                client.Close();
                listener.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private Bitmap CaptureScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen!.Bounds;
            Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
            }

            return screenshot;
        }

        private void SendFullFrame(Bitmap frame, NetworkStream stream)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                frame.Save(ms, ImageFormat.Jpeg);
                byte[] buffer = ms.ToArray();

                // Send size of image
                stream.Write(BitConverter.GetBytes(buffer.Length), 0, 4);

                // Send image
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        private void SendDirtyRegions(Bitmap currentFrame, Bitmap previousFrame, NetworkStream stream)
        {
            // Compare pixels between current and previous frames
            for (int y = 0; y < currentFrame.Height; y++)
            {
                for (int x = 0; x < currentFrame.Width; x++)
                {
                    if (currentFrame.GetPixel(x, y) != previousFrame.GetPixel(x, y))
                    {
                        Rectangle dirtyRegion = new Rectangle(x, y, 1, 1); // Minimal region size
                        using (Bitmap dirtyBitmap = currentFrame.Clone(dirtyRegion, currentFrame.PixelFormat))
                        {
                            SendFullFrame(dirtyBitmap, stream);
                        }
                    }
                }
            }
        }
    }
}
