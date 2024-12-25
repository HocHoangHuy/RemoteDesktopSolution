using System;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using System.Runtime.InteropServices;
using Device = SharpDX.Direct3D11.Device;

public class ScreenCapture
{
    private readonly Device _device;
    private readonly Texture2D _screenTexture;
    private readonly Factory1 _factory;
    private readonly Output1 _output1;
    private bool _capturing = true;
    private OutputDuplication duplicatedOutput;

    public ScreenCapture()
    {
        // Initialize Direct3D11 Device and Factory1
        _factory = new Factory1();
        var adapter = _factory.GetAdapter1(0);  // Get the first display adapter
        _device = new Device(adapter);

        // Set up output (first monitor in this case)
        var output = adapter.GetOutput(0);
        _output1 = output.QueryInterface<Output1>();

        // Set up texture description for the screen capture texture
        var outputDescription = output.Description;
        var textureDesc = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = outputDescription.DesktopBounds.Right,
            Height = outputDescription.DesktopBounds.Bottom,
            Usage = ResourceUsage.Staging,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = new SampleDescription(1, 0),
            OptionFlags = ResourceOptionFlags.None
        };

        _screenTexture = new Texture2D(_device, textureDesc);
        duplicatedOutput = _output1.DuplicateOutput(_device);
    }

    // Method that returns a Bitmap object of the current screen capture
    public Bitmap GetCapturedFrame()
    {
        // Use Task to capture screen continuously in a separate thread

        {
            try
            {
                // Capture the next frame from the duplicated output
                SharpDX.DXGI.Resource screenResource;
                OutputDuplicateFrameInformation duplicateFrameInformation;

                // Try to acquire the next frame within 5ms timeout
                Result result;
                do
                {
                    result = duplicatedOutput.TryAcquireNextFrame(5, out duplicateFrameInformation, out screenResource);
                }
                while (result != Result.Ok);
                // Copy the screen texture into the staging texture that can be accessed by CPU
                var screenTexture2D = screenResource.QueryInterface<Texture2D>();
                _device.ImmediateContext.CopyResource(screenTexture2D, _screenTexture);
                screenTexture2D.Dispose();


                // Map the texture so that we can access its data
                var mapSource = _device.ImmediateContext.MapSubresource(_screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                // Convert the texture to Bitmap and return it
                Bitmap bitmap = TextureToBitmap(mapSource.DataPointer, _screenTexture.Description.Width, _screenTexture.Description.Height);
                // Release resources
                _device.ImmediateContext.UnmapSubresource(_screenTexture, 0);
                duplicatedOutput.ReleaseFrame();
                screenResource.Dispose();

                return bitmap;  // Return the captured bitmap

            }
            catch (SharpDXException e)
            {
                Console.WriteLine("Error while capturing screen: " + e.Message);
                return null;
            }
        }
    }

    private Bitmap TextureToBitmap(IntPtr dataPointer, int width, int height)
    {
        // Convert the captured texture (raw data) to a Bitmap (32bpp ARGB)
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var boundsRect = new System.Drawing.Rectangle(0, 0, width, height);

        var bitmapData = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
        Utilities.CopyMemory(bitmapData.Scan0, dataPointer, width * height * 4);
        bitmap.UnlockBits(bitmapData);

        return bitmap;
    }

    public void StopCapture()
    {
        _capturing = false;
    }
}


