// See https://aka.ms/new-console-template for more information
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Form;

namespace StreamScreen;

public class Program
{
    public static void Main()
    {
        bool _isStreaming = true;
        TcpListener listener = new TcpListener(IPAddress.Any, 8888);
        listener.Start();
        TcpClient client;
        try
        {
            client = listener.AcceptTcpClient();
        }
        catch (Exception) { return; }
        NetworkStream stream = client.GetStream();

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

    private Bitmap CaptureScreen()
    {
        Rectangle bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
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


