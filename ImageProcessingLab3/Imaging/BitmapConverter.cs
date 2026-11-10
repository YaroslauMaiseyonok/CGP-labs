using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessingLab3.Imaging;

public static class BitmapConverter
{
    public static ImageBuffer FromBitmap(Bitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }

        using var argb = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(argb))
        {
            graphics.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
        }

        var buffer = new ImageBuffer(argb.Width, argb.Height);

        var bitmapData = argb.LockBits(
            new Rectangle(0, 0, argb.Width, argb.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            unsafe
            {
                byte* source = (byte*)bitmapData.Scan0;

                fixed (byte* destination = buffer.Data)
                {
                    for (int y = 0; y < buffer.Height; y++)
                    {
                        byte* sourceRow = source + y * bitmapData.Stride;
                        byte* destinationRow = destination + y * buffer.Stride;

                        for (int x = 0; x < buffer.Width; x++)
                        {
                            int offset = x * 4;

                            byte blue = sourceRow[offset];
                            byte green = sourceRow[offset + 1];
                            byte red = sourceRow[offset + 2];

                            destinationRow[x] = (byte)((red * 38 + green * 75 + blue * 15) >> 7);
                        }
                    }
                }
            }
        }
        finally
        {
            argb.UnlockBits(bitmapData);
        }

        return buffer;
    }

    public static Bitmap ToBitmap(ImageBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        var bitmap = new Bitmap(buffer.Width, buffer.Height, PixelFormat.Format8bppIndexed);
        SetGrayscalePalette(bitmap);

        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, buffer.Width, buffer.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format8bppIndexed);

        try
        {
            unsafe
            {
                fixed (byte* source = buffer.Data)
                {
                    byte* destination = (byte*)bitmapData.Scan0;

                    for (int y = 0; y < buffer.Height; y++)
                    {
                        Buffer.MemoryCopy(
                            (void*)(source + y * buffer.Stride),
                            (void*)(destination + y * bitmapData.Stride),
                            bitmapData.Stride,
                            buffer.Stride);
                    }
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    private static void SetGrayscalePalette(Bitmap bitmap)
    {
        var palette = bitmap.Palette;

        for (int i = 0; i < 256; i++)
        {
            palette.Entries[i] = Color.FromArgb(i, i, i);
        }

        bitmap.Palette = palette;
    }
}