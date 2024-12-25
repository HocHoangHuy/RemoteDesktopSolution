// See https://aka.ms/new-console-template for more information
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Sockets;

namespace Client;

public class Program
{

    private static Thread StartCaptureScreen()
    {
        Thread captureScreen = new Thread((networkStream) =>
        {
            NetworkStream network = (NetworkStream)networkStream!;
            {
                ScreenCapturer screenCapturer = new();
                MemoryStream memoryStream;
                memoryStream = new MemoryStream();

                while (Thread.CurrentThread.IsAlive)
                {
                    Bitmap screenBitmap = screenCapturer.CaptureFrame();
                    memoryStream.Position = 0;
                    screenBitmap.Save(memoryStream, ImageFormat.Jpeg);
                    network.Write(BitConverter.GetBytes(memoryStream.Length));
                    network.Write(memoryStream.ToArray());
                }
            }
        });
        captureScreen.Name = "CaptureScreenThread";
        captureScreen.Start();
        return captureScreen;
    }

    public static void Main()
    {
        TcpListener tcpListener = new TcpListener(System.Net.IPAddress.Any, 8888);
        tcpListener.Start();
        TcpClient tcpClient = tcpListener.AcceptTcpClient();

    }
}

