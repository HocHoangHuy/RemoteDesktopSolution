// See https://aka.ms/new-console-template for more information
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Sockets;

namespace Client;

public class Program
{

    private static Thread StartCaptureScreen(TcpClient client)
    {
        Thread captureScreen = new Thread((networkStream) =>
        {
            try
            {
                NetworkStream network = ((TcpClient)networkStream!).GetStream();
                {
                    ScreenCapturer screenCapturer = new();
                    MemoryStream memoryStream;
                    memoryStream = new MemoryStream();

                    while (Thread.CurrentThread.IsAlive)
                    {
                        Bitmap screenBitmap = screenCapturer.CaptureFrame();
                        memoryStream.Position = 0;
                        screenBitmap.Save(memoryStream, ImageFormat.Jpeg);
                        network.Write(BitConverter.GetBytes((int)memoryStream.Length));
                        network.Write(memoryStream.ToArray());

                    }
                }
            }
            catch (SocketException)
            {
                ((TcpClient)networkStream).Close();
            }

            catch (IOException e)
            {
                if (e.InnerException is SocketException)
                    ((TcpClient)networkStream).Close();
            }
        });
        captureScreen.Name = "CaptureScreenThread";
        captureScreen.Start(client);
        return captureScreen;
    }

    public static void Main()
    {
        TcpListener tcpListener = new TcpListener(System.Net.IPAddress.Any, 8888);
        tcpListener.Start();
        while(true)
        {
            TcpClient tcpClient = tcpListener.AcceptTcpClient();
            StartCaptureScreen(tcpClient);
        }
        
        
    }
}

