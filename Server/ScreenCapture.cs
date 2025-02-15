﻿using System;
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
using static System.Net.Mime.MediaTypeNames;
using System.Collections;
using System.Diagnostics;

public class ScreenCapture
{
    private readonly Device _device;
    private readonly Texture2D _screenTexture;
    private readonly Factory1 _factory;
    private readonly Output1 _output1;
    private bool _capturing = true;
    private OutputDuplication duplicatedOutput;
    private byte[] pointerShapeBuffer;
    private OutputDuplicatePointerShapeInformation? pointerInfo;
    private RawPoint pointerTopLeftPosition, pointerHotspot;

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
        pointerShapeBuffer = Array.Empty<byte>();
        while (true)
        {
            if (!GetCursorPos(out pointerHotspot)) continue; else break;
        }
        pointerTopLeftPosition = new RawPoint(0, 0);
    }

    public Size GetScreenSize()
    {
        // Get the screen size of the primary monitor
        var outputDescription = _output1.Description.DesktopBounds;
        return new Size(outputDescription.Right, outputDescription.Bottom);
    }

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out RawPoint lpPoint);

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
                Result result = duplicatedOutput.TryAcquireNextFrame(5, out duplicateFrameInformation, out screenResource);
                if (!result.Success) return null;
                // Copy the screen texture into the staging texture that can be accessed by CPU
                var screenTexture2D = screenResource.QueryInterface<Texture2D>();
                _device.ImmediateContext.CopyResource(screenTexture2D, _screenTexture);
                screenTexture2D.Dispose();


                // Map the texture so that we can access its data
                var mapSource = _device.ImmediateContext.MapSubresource(_screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                // Convert the texture to Bitmap and return it
                Bitmap bitmap = TextureToBitmap(mapSource.DataPointer, _screenTexture.Description.Width, _screenTexture.Description.Height);
                // Process cursor (mouse pointer)
                if (duplicateFrameInformation.PointerShapeBufferSize > 0)
                {
                    pointerShapeBuffer = new byte[duplicateFrameInformation.PointerShapeBufferSize];
                    GCHandle handle = GCHandle.Alloc(pointerShapeBuffer, GCHandleType.Pinned);
                    IntPtr pointerShapeBufferRef;
                    try
                    {
                        // Get the pointer to the array
                        pointerShapeBufferRef = handle.AddrOfPinnedObject();
                    }
                    finally
                    {
                        // Free the handle when done
                        handle.Free();
                    }
                    OutputDuplicatePointerShapeInformation _pointerInfo;
                    duplicatedOutput.GetFramePointerShape(pointerShapeBuffer.Length, pointerShapeBufferRef, out int pointerShapeBufferRequired, out _pointerInfo);
                    pointerInfo = _pointerInfo;
                }

                GetCursorPos(out pointerHotspot);
                pointerTopLeftPosition.X = pointerHotspot.X - pointerInfo?.HotSpot.X ?? 0;
                pointerTopLeftPosition.Y = pointerHotspot.Y - pointerInfo?.HotSpot.Y ?? 0;
                if (pointerInfo != null)
                    DrawCursorOnBitmap(bitmap, pointerInfo.Value, pointerTopLeftPosition, pointerShapeBuffer);


                // Release resources
                _device.ImmediateContext.UnmapSubresource(_screenTexture, 0);
                duplicatedOutput.ReleaseFrame();
                screenResource.Dispose();

                //bitmap.Save("C:/Users/Admin/screen.jpg", ImageFormat.Jpeg);

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

    private Bitmap ConvertMonochromePointerToBitmap(byte[] buffer, int width, int height)
    {
        var cursorBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var cursorData = cursorBitmap.LockBits(
            new System.Drawing.Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Extract monochrome mask and XOR pattern to calculate pixel color
                int byteIndex = y * ((width + 7) / 8) + (x / 8);
                bool isWhite = (buffer[byteIndex] & (0x80 >> (x % 8))) != 0;

                cursorBitmap.SetPixel(x, y, isWhite ? System.Drawing.Color.White : System.Drawing.Color.Black);
            }
        }

        cursorBitmap.UnlockBits(cursorData);
        return cursorBitmap;

    }


    private Bitmap ConvertColorPointerToBitmap(byte[] buffer, int width, int height)
    {
        Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        // Lock bitmap for writing
        var bitmapData = bitmap.LockBits(
            new System.Drawing.Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        // Copy the RGBA data into the bitmap
        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bitmapData.Scan0, buffer.Length);

        // Unlock bitmap
        bitmap.UnlockBits(bitmapData);

        return bitmap;
    }

    private void DrawCursorOnBitmap(Bitmap bitmap, OutputDuplicatePointerShapeInformation pointerShapeInformation, RawPoint cursorTopLeftPosition, byte[] pointerShapeBuffer)
    {
        using (var graphics = Graphics.FromImage(bitmap))
        {
            var cursorWidth = pointerShapeInformation.Width;
            var cursorHeight = pointerShapeInformation.Height;
            var hotspotX = pointerShapeInformation.HotSpot.X;
            var hotspotY = pointerShapeInformation.HotSpot.Y;
            Bitmap cursorBitmap;
            //Task.Delay(5000);
            //Debugger.Break();
            // Convert pointer shape buffer to an image (example assumes monochrome bitmap)
            switch (pointerShapeInformation.Type)
            {
                case (int)OutputDuplicatePointerShapeType.Monochrome:
                    cursorBitmap = ConvertMonochromePointerToBitmap(pointerShapeBuffer, cursorWidth, cursorHeight);
                    break;
                case (int)OutputDuplicatePointerShapeType.Color:
                    cursorBitmap = ConvertColorPointerToBitmap(pointerShapeBuffer, cursorWidth, cursorHeight);
                    break;
                case (int)OutputDuplicatePointerShapeType.MaskedColor:
                    cursorBitmap = ConvertMaskedToBitmap(bitmap, pointerShapeInformation, pointerShapeBuffer, cursorTopLeftPosition);
                    break;
                default: return;
            }

            graphics.DrawImage(cursorBitmap, cursorTopLeftPosition.X, cursorTopLeftPosition.Y);
            //cursorBitmap.Save("C:\\Users\\satos\\cursor.jpg", ImageFormat.Jpeg);
        }
    }

    public Bitmap ConvertMaskedToBitmap(Bitmap screenBitmap, OutputDuplicatePointerShapeInformation pointerShapeInfo, byte[] pointerShapeBuffer, RawPoint cursorTopLeftPosition)
    {
        int width = pointerShapeInfo.Width;
        int height = pointerShapeInfo.Height;
        int pitch = pointerShapeInfo.Pitch;

        // Create a bitmap to store the cursor
        Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        // Iterate over each pixel in the cursor shape
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * pitch + (x * 4);
                byte blue = pointerShapeBuffer[index];
                byte green = pointerShapeBuffer[index + 1];
                byte red = pointerShapeBuffer[index + 2];
                byte alpha = pointerShapeBuffer[index + 3];

                bool isTransparent = alpha == 0xFF;

                // Set pixel in the bitmap
                System.Drawing.Color color;
                if (isTransparent)
                {
                    System.Drawing.Color screenPixelColor = screenBitmap.GetPixel(cursorTopLeftPosition.X + x, cursorTopLeftPosition.Y + y);
                    color = System.Drawing.Color.FromArgb(255, screenPixelColor.R ^ red, screenPixelColor.G ^ green, screenPixelColor.B ^ blue);
                }
                else
                {
                    if ((blue + green + red) == 0)
                        color = System.Drawing.Color.FromArgb(0, 0, 0, 0);
                    else
                        color = System.Drawing.Color.FromArgb(255, red, green, blue);
                }

                bitmap.SetPixel(x, y, color);
            }
        }

        return bitmap;
    }
}


