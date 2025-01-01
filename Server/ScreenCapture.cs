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
        while(true)
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
                //// Process cursor (mouse pointer)
                //if (duplicateFrameInformation.PointerShapeBufferSize > 0)
                //{
                //    pointerShapeBuffer = new byte[duplicateFrameInformation.PointerShapeBufferSize];
                //    GCHandle handle = GCHandle.Alloc(pointerShapeBuffer, GCHandleType.Pinned);
                //    IntPtr pointerShapeBufferRef;
                //    try
                //    {
                //        // Get the pointer to the array
                //        pointerShapeBufferRef = handle.AddrOfPinnedObject();
                //    }
                //    finally
                //    {
                //        // Free the handle when done
                //        handle.Free();
                //    }
                //    OutputDuplicatePointerShapeInformation _pointerInfo;
                //    duplicatedOutput.GetFramePointerShape(pointerShapeBuffer.Length, pointerShapeBufferRef, out int pointerShapeBufferRequired, out _pointerInfo);
                //    pointerInfo = _pointerInfo;
                //}

                //RawPoint newCursorPos;
                //GetCursorPos(out newCursorPos);

                //if (!newCursorPos.Equals(pointerHotspot))
                //{
                //    pointerTopLeftPosition = duplicateFrameInformation.PointerPosition.Position;
                //    pointerHotspot = newCursorPos;
                //}
                //if (pointerInfo != null)
                //DrawCursorOnBitmap(bitmap, pointerInfo.Value, pointerTopLeftPosition, pointerShapeBuffer);


                // Release resources
                _device.ImmediateContext.UnmapSubresource(_screenTexture, 0);
                duplicatedOutput.ReleaseFrame();
                screenResource.Dispose();

                bitmap.Save("C:/Users/Admin/screen.jpg", ImageFormat.Jpeg);

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
                    cursorBitmap = ConvertMaskedToBitmap(pointerShapeInformation, pointerShapeBuffer);
                    break;
                default: return;
            }

            graphics.DrawImage(cursorBitmap, cursorTopLeftPosition.X, cursorTopLeftPosition.Y);
            bitmap.Save("C:\\Users\\satos\\screen.jpg", ImageFormat.Jpeg);
        }
    }

    //private Bitmap ConvertMaskedToBitmap(byte[] buffer, int width, int height)
    //{
    //    // Create a bitmap to hold the final result
    //    Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

    //    // Step 1: Create a graphics object to manipulate the bitmap
    //    using (Graphics g = Graphics.FromImage(bitmap))
    //    {
    //        // Clear the bitmap with a transparent color
    //        g.Clear(System.Drawing.Color.Transparent);

    //        // Step 2: Extract AND mask (binary) and XOR mask (color)
    //        // Assuming buffer contains the combined AND and XOR mask data
    //        int andMaskSize = (width * height + 7) / 8; // Size of the AND mask
    //        int xorMaskSize = (width * height * 4); // Size of the XOR mask (32-bit color for each pixel)

    //        if (andMaskSize + xorMaskSize != buffer.Length)
    //        {
    //            var adjustedSize = AdjustDimensionsToFitBufferSize(buffer.Length);
    //            width = adjustedSize.width;
    //            height = adjustedSize.height;
    //            andMaskSize = (width * height + 7) / 8;
    //            xorMaskSize = (width * height * 4);
    //        }

    //        byte[] andMask = new byte[andMaskSize];
    //        byte[] xorMask = new byte[xorMaskSize];

    //        // Copy the AND and XOR mask data from the buffer
    //        Array.Copy(buffer, 0, andMask, 0, andMaskSize);      // First part is the AND mask
    //        Array.Copy(buffer, andMaskSize, xorMask, 0, xorMaskSize);  // Next part is the XOR mask

    //        // Step 3: Process the AND mask and XOR mask to create the final bitmap
    //        BitmapData lockedBits = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
    //        int byteIndex = 0;
    //        for (int y = 0; y < height; y++)
    //        {
    //            for (int x = 0; x < width; x++)
    //            {
    //                // Get the corresponding bit in the AND mask
    //                int bitIndex = (y * width) + x;
    //                int bytePos = bitIndex / 8;
    //                int bitPos = bitIndex % 8;
    //                bool isOpaque = (andMask[bytePos] & (1 << (7 - bitPos))) != 0;

    //                // Get the corresponding color in the XOR mask (ARGB)
    //                int colorPos = (y * width + x) * 4;
    //                byte blue = xorMask[colorPos];
    //                byte green = xorMask[colorPos + 1];
    //                byte red = xorMask[colorPos + 2];
    //                byte alpha = xorMask[colorPos + 3];

    //                // If the AND mask indicates the cursor is visible, use the XOR mask for color
    //                if (isOpaque)
    //                {
    //                    System.Drawing.Color color = System.Drawing.Color.FromArgb(alpha, red, green, blue);
    //                    bitmap.SetPixel(x, y, color);
    //                }
    //                else
    //                {
    //                    // The pixel is transparent (due to AND mask), set to transparent
    //                    bitmap.SetPixel(x, y, System.Drawing.Color.Transparent);
    //                }
    //            }
    //        }
    //        bitmap.UnlockBits(lockedBits);
    //    }

    //    bitmap.Save("C:\\Users\\satos\\cursor.jpg", ImageFormat.Jpeg);

    //    return bitmap;
    //}

    public Bitmap ConvertMaskedToBitmap(OutputDuplicatePointerShapeInformation pointerShapeInfo, byte[] pointerShapeBuffer)
    {
        int width = pointerShapeInfo.Width;
        int height = pointerShapeInfo.Height;
        int pitch = pointerShapeInfo.Pitch;

        // Create a bitmap to store the cursor
        Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        // Lock the bitmap for fast pixel manipulation
        //BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            //unsafe
            {
                //byte* destPixels = (byte*)bitmapData.Scan0;
                int bufferIndex = 0;

                // Iterate over each pixel in the cursor shape
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Extract color from the color mask
                        byte blue = pointerShapeBuffer[bufferIndex];
                        byte green = pointerShapeBuffer[bufferIndex + 1];
                        byte red = pointerShapeBuffer[bufferIndex + 2];
                        bufferIndex += 4; // Move to the next color pixel (RGBA format)

                        // Extract transparency from the AND mask
                        int maskOffset = y * pitch + (x / 8); // Each byte in the AND mask covers 8 pixels
                        bool isTransparent = ((pointerShapeBuffer[maskOffset] >> (7 - (x % 8))) & 1) == 0;

                        // Set pixel in the bitmap
                        bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(((pointerShapeBuffer[maskOffset] >> (7 - (x % 8))) & 1), red, green, blue));
                        //int pixelIndex = (y * bitmapData.Stride) + (x * 4);
                        //destPixels[pixelIndex] = blue;
                        //destPixels[pixelIndex + 1] = green;
                        //destPixels[pixelIndex + 2] = red;
                        //destPixels[pixelIndex + 3] = isTransparent ? (byte)0 : (byte)255; // Apply transparency
                    }
                }
            }
        }
        finally
        {
            // Unlock the bitmap
            //bitmap.UnlockBits(bitmapData);
        }

        //bitmap.Save("C:\\Users\\satos\\cursor.jpg", ImageFormat.Jpeg);

        return bitmap;
    }


    //public (int width, int height) AdjustDimensionsToFitBufferSize(int bufferSize)
    //{
    //    // Try different potential widths and heights
    //    for (int width = 1; width <= 256; width++) // Adjust the range based on expected cursor size
    //    {
    //        for (int height = 1; height <= 256; height++)
    //        {
    //            // Calculate the size of the AND and XOR masks
    //            int andMaskSize = (width * height + 7) / 8;  // AND mask size in bytes
    //            int xorMaskSize = width * height * 4;        // XOR mask size in bytes (32-bit ARGB)

    //            // Total buffer size
    //            int totalSize = andMaskSize + xorMaskSize;

    //            // If the total buffer size matches the provided buffer size, return the dimensions
    //            if (totalSize == bufferSize)
    //            {
    //                return (width, height); // Found matching width and height
    //            }
    //        }
    //    }

    //    // If no matching dimensions are found, return an indication (e.g., -1 for width and height)
    //    return (-1, -1); // Return an invalid dimension pair if no match is found
    //}
}


