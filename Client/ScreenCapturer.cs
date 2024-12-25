using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.DXGI.Resource;

public class ScreenCapturer
{
    private Device device;
    private OutputDuplication outputDuplication;
    private Texture2D desktopTexture;

    public ScreenCapturer()
    {
        // Initialize DirectX Device and Output Duplication
        InitializeDirectX();
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    private void InitializeDirectX()
    {
        // Create the DirectX Device
        device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.None);

        // Get DXGI Adapter
        using var factory = new Factory1();
        var adapter = factory.GetAdapter1(0);

        // Get the output (screen)
        var output = adapter.GetOutput(0);
        var output1 = output.QueryInterface<Output1>();

        // Create Output Duplication
        outputDuplication = output1.DuplicateOutput(device);

        // Get the desktop texture description
        var desktopDesc = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = GetSystemMetrics(SM_CXSCREEN),
            Height = GetSystemMetrics(SM_CYSCREEN),
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging
        };

        desktopTexture = new Texture2D(device, desktopDesc);
    }

    public Bitmap CaptureFrame()
    {
        try
        {
            // Acquire next frame
            var frameInfo = outputDuplication.TryAcquireNextFrame(500, out var frameInfoRef, out var resource);
            if (frameInfo != Result.Ok)
            {
                throw new Exception("Failed to acquire frame.");
            }

            // Copy resource to the desktop texture
            using var texture = resource.QueryInterface<Texture2D>();
            device.ImmediateContext.CopyResource(texture, desktopTexture);

            // Map the resource to read pixel data
            var dataBox = device.ImmediateContext.MapSubresource(desktopTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
            var bitmap = new Bitmap(desktopTexture.Description.Width, desktopTexture.Description.Height, PixelFormat.Format32bppArgb);

            // Copy data to Bitmap
            var bounds = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Utilities.CopyMemory(bitmapData.Scan0, dataBox.DataPointer, bitmapData.Height * bitmapData.Stride);
            bitmap.UnlockBits(bitmapData);

            // Unmap the resource
            device.ImmediateContext.UnmapSubresource(desktopTexture, 0);

            // Release the frame
            outputDuplication.ReleaseFrame();

            return bitmap;
        }
        catch (SharpDXException ex)
        {
            Debug.WriteLine($"Error during screen capture: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        desktopTexture?.Dispose();
        outputDuplication?.Dispose();
        device?.Dispose();
    }
}
