using System;

namespace ImageProcessingLab3.Imaging;

public sealed class ImageBuffer
{
    public ImageBuffer(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;

        Stride = width;

        Data = new byte[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public int Stride { get; }

    public byte[] Data { get; }

    public ImageBuffer Clone()
    {
        var clone = new ImageBuffer(Width, Height);
        Buffer.BlockCopy(Data, 0, clone.Data, 0, Data.Length);
        return clone;
    }
}