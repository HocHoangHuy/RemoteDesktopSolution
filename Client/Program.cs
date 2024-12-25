// See https://aka.ms/new-console-template for more information
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;
using System.Net.Sockets;

namespace Client;

public class Program
{
    private static ScreenCapture ScreenStateLogger = new();

    private static Thread StartCaptureScreen(TcpClient client)
    {
        Thread captureScreen = new Thread((networkStream) =>
        {
            try
            {
                NetworkStream network = ((TcpClient)networkStream!).GetStream();
                {
                    
                    MemoryStream memoryStream;
                    memoryStream = new MemoryStream();
                    Bitmap recentScreenBitmap;
                    do
                    {
                        recentScreenBitmap = ScreenStateLogger.GetCapturedFrame();
                    } while (recentScreenBitmap == null);

                    while (Thread.CurrentThread.IsAlive)
                    {
                        Bitmap screenBitmap = ScreenStateLogger.GetCapturedFrame();
                        if (screenBitmap == null) screenBitmap = recentScreenBitmap;
                        else recentScreenBitmap = screenBitmap;
                        memoryStream = new MemoryStream();
                        screenBitmap.Save(memoryStream, ImageFormat.Jpeg);
                        network.Write(BitConverter.GetBytes((int)memoryStream.Length));
                        network.Write(memoryStream.ToArray());
                        Thread.Sleep(1000/30);
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

