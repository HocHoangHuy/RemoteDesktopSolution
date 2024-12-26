// See https://aka.ms/new-console-template for more information
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Client;

public class Program
{
    private static ScreenCapture ScreenCapturer = new();

    private static Thread StartCaptureScreen(TcpClient client)
    {
        Thread captureScreen = new Thread((tcpClient) =>
        {
            try
            {
                NetworkStream network = ((TcpClient)tcpClient!).GetStream();
                {

                    MemoryStream memoryStream;
                    memoryStream = new MemoryStream();
                    Bitmap recentScreenBitmap;
                    do
                    {
                        recentScreenBitmap = ScreenCapturer.GetCapturedFrame();
                    } while (recentScreenBitmap == null);

                    while (Thread.CurrentThread.IsAlive)
                    {
                        Bitmap screenBitmap = ScreenCapturer.GetCapturedFrame();
                        if (screenBitmap == null) screenBitmap = recentScreenBitmap;
                        else recentScreenBitmap = screenBitmap;
                        memoryStream = new MemoryStream();
                        screenBitmap.Save(memoryStream, ImageFormat.Jpeg);
                        network.Write(BitConverter.GetBytes((int)memoryStream.Length));
                        network.Write(memoryStream.ToArray());
                    }
                }
            }
            catch (SocketException)
            {
                ((TcpClient)tcpClient).Close();
            }

            catch (IOException e)
            {
                if (e.InnerException is SocketException)
                    ((TcpClient)tcpClient).Close();
            }
        });
        captureScreen.Name = "CaptureScreenThread";
        captureScreen.Start(client);
        Thread receive = new((tcpClient) =>
        {
            try
            {
                NetworkStream network = ((TcpClient)tcpClient!).GetStream();
                Size screenSize = ScreenCapturer.GetScreenSize();
                byte command;
                while (true)
                {
                    command = (byte)network.ReadByte();
                    if (command == 1)
                    {
                        double x_percent, y_percent;
                        using (BinaryReader reader = new(network, Encoding.UTF8, true))
                        {
                            x_percent = reader.ReadDouble();
                            y_percent = reader.ReadDouble();
                        }
                        int x = (int)(x_percent * screenSize.Width);
                        int y = (int)(y_percent * screenSize.Height);
                        SimulateMouseClick(x, y);
                    }
                }

            }
            catch (SocketException)
            {
                ((TcpClient)tcpClient).Close();
            }

            catch (IOException e)
            {
                if (e.InnerException is SocketException)
                    ((TcpClient)tcpClient).Close();
            }
        });
        return captureScreen;
    }

    public static void Main()
    {
        TcpListener tcpListener = new TcpListener(System.Net.IPAddress.Any, 8888);
        tcpListener.Start();
        while (true)
        {
            TcpClient tcpClient = tcpListener.AcceptTcpClient();
            StartCaptureScreen(tcpClient);
        }


    }

    // Import the mouse_event function from the user32.dll
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

    // Mouse event constants
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002; // Left button down
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;   // Left button up
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008; // Right button down
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;   // Right button up

    private static void SimulateMouseClick(int dx, int dy)
    {
        // Simulate left button down and up (click)
        mouse_event(MOUSEEVENTF_LEFTDOWN, dx, dy, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_LEFTUP, dx, dy, 0, UIntPtr.Zero);
        Console.WriteLine("Mouse click triggered!");
    }
}

