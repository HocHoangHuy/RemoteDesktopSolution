// See https://aka.ms/new-console-template for more information
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Client;

public partial class Program
{
    private static BlockingCollection<Action> MouseActions = new(new ConcurrentQueue<Action>());
    private static BlockingCollection<Action> KeyboardActions = new(new ConcurrentQueue<Action>());

    private static ScreenCapture ScreenCapturer = new();

    private static void StartCaptureScreen()
    {
        Task captureScreen = new Task(() =>
        {
            Thread.CurrentThread.Name = "CaptureScreenThread";
            TcpListener tcpListener = new TcpListener(System.Net.IPAddress.Any, 8888);
            tcpListener.Start();
            TcpClient tcpClient = tcpListener.AcceptTcpClient();
            tcpListener.Stop();
            try
            {
                using (NetworkStream network = tcpClient.GetStream())
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
                tcpClient.Close();
            }

            catch (IOException e)
            {
                if (e.InnerException is SocketException)
                    tcpClient.Close();
            }
        }, TaskCreationOptions.LongRunning);
        captureScreen.Start();
        Task receiveMouse = new(() =>
        {
            Thread.CurrentThread.Name = "ReceiveMouseThread";
            TcpListener tcpListener = new TcpListener(System.Net.IPAddress.Any, 8889);
            tcpListener.Start();
            TcpClient tcpClient = tcpListener.AcceptTcpClient();
            tcpListener.Stop();
            Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.Name = "ExecuteMouseThread";
                while (!MouseActions.IsCompleted)
                {
                    try
                    {
                        MouseActions.Take().Invoke();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }, TaskCreationOptions.LongRunning);
            try
            {
                NetworkStream network = tcpClient.GetStream();
                Size screenSize = ScreenCapturer.GetScreenSize();
                byte command;
                while (true)
                {
                    command = (byte)network.ReadByte();
                    //if (command == 1)
                    //{
                    //    double x_percent, y_percent;
                    //    using (BinaryReader reader = new(network, Encoding.UTF8, true))
                    //    {
                    //        x_percent = reader.ReadDouble();
                    //        y_percent = reader.ReadDouble();
                    //    }
                    //    int x = (int)(x_percent * screenSize.Width);
                    //    int y = (int)(y_percent * screenSize.Height);
                    //    LeftClick();
                    //}

                    switch (command)
                    {
                        case 1: // Move mouse
                            double x_percent, y_percent;
                            using (BinaryReader reader = new(network, Encoding.UTF8, true))
                            {
                                x_percent = reader.ReadDouble();
                                y_percent = reader.ReadDouble();
                            }
                            MouseActions.Add(()=>MoveMouseAbsolutePosition(x_percent, y_percent));
                            break;
                        case 2: //Left down
                            MouseActions.Add(LeftDown);
                            break;
                        case 3: //Left up
                            MouseActions.Add(LeftUp);
                            break;
                        case 4: //Right down
                            MouseActions.Add(RightDown);
                            break;

                        case 5: //Right up
                            MouseActions.Add(RightUp);
                            break;
                        case 6: //Middle down
                            MouseActions.Add(MiddleDown);
                            break;
                        case 7: //Middle up
                            MouseActions.Add(MiddleUp);
                            break;
                        case 8: //Wheel
                            int clicks;
                            using (BinaryReader reader = new(network, Encoding.UTF8, true))
                            {
                                clicks = reader.ReadInt32();
                            }
                            MouseActions.Add(() => Wheel(clicks));
                            break;
                        //case 9: //HWheel
                        //    int hclicks;
                        //    using (BinaryReader reader = new(network, Encoding.UTF8, true))
                        //    {
                        //        hclicks = reader.ReadInt32();
                        //    }
                        //    MouseActions.Add(() => HWheel(hclicks));
                        //    break;
                        default:
                            break;
                    }
                }

            }
            catch (SocketException)
            {
                tcpClient.Close();
            }

            catch (IOException e)
            {
                if (e.InnerException is SocketException)
                    tcpClient.Close();
            }
        }, TaskCreationOptions.LongRunning);
        receiveMouse.Start();

    }

    public static void Main()
    {
        StartCaptureScreen();
        //TcpListener tcpListener = new TcpListener(System.Net.IPAddress.Any, 8888);
        //tcpListener.Start();
        //while (true)
        //{
        //    TcpClient tcpClient = tcpListener.AcceptTcpClient();
        //    StartCaptureScreen(tcpClient);
        //}

        //SetCursorPos(1280, 720);
        //double x_percent = 0, y_percent = 0;
        //SetCursorPos(1270, 700);
        //for (int i = 0; i <= 100; i++)
        //{
        //    mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, (uint)(i * 65535/100 ), (uint)(i * 65535/100), 0, UIntPtr.Zero);
        //    Thread.Sleep(1);
        //}



    }

}

partial class Program
{
    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    static extern void mouse_event(uint dwFlags, uint dx, uint dy, int dwData, UIntPtr dwExtraInfo);

    // Mouse event constants
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_XDOWN = 0x0080;
    private const uint MOUSEEVENTF_XUP = 0x0100;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_HWHEEL = 0x01000;
    private const int WHEEL_DELTA = 120;

    // Function to move the mouse to a specific position
    static void MoveMouseAbsolutePosition(double xPercent, double yPercent)
    {
        mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, (uint)(xPercent * 65535), (uint)(yPercent * 65535), 0, UIntPtr.Zero);
    }

    // Function to simulate a left mouse click
    static void LeftDown()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
    }

    static void LeftUp()
    {
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }

    static void RightDown()
    {
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
    }

    static void RightUp()
    {
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
    }

    static void MiddleDown()
    {
        mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
    }

    static void MiddleUp()
    {
        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
    }

    static void Wheel(int clicks)
    {
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, clicks * WHEEL_DELTA, UIntPtr.Zero);
    }
}

