using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ImageProcessingLab3.Imaging;


public sealed class IntegralImage
{
    private IntegralImage(int width, int height)
    {
        Width = width;
        Height = height;
        Sum = new long[width * height];
        SumSq = new long[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public long[] Sum { get; }

    public long[] SumSq { get; }

    public static IntegralImage Compute(ImageBuffer source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var integral = new IntegralImage(source.Width, source.Height);

        unsafe
        {

            fixed (byte* pSource = source.Data)
            fixed (long* pSum = integral.Sum)
            fixed (long* pSumSq = integral.SumSq)
            {
                IntPtr sourcePtr = (IntPtr)(void*)pSource;
                IntPtr sumPtr = (IntPtr)(void*)pSum;
                IntPtr sumSqPtr = (IntPtr)(void*)pSumSq;

                Parallel.For(0, source.Height, y =>
                {
                    ComputeHorizontalRow(
                        sourcePtr,
                        sumPtr,
                        sumSqPtr,
                        y,
                        source.Width,
                        source.Stride,
                        integral.Width);
                });
            }

            fixed (long* pSum = integral.Sum)
            fixed (long* pSumSq = integral.SumSq)
            {
                for (int y = 1; y < integral.Height; y++)
                {
                    int row = y * integral.Width;
                    int previousRow = row - integral.Width;

                    for (int x = 0; x < integral.Width; x++)
                    {
                        pSum[row + x] += pSum[previousRow + x];
                        pSumSq[row + x] += pSumSq[previousRow + x];
                    }
                }
            }
        }

        return integral;
    }

    public static void GetWindowCoordinates(
        int x,
        int y,
        int windowSize,
        int width,
        int height,
        out int x1,
        out int y1,
        out int x2,
        out int y2)
    {
        int halfWindow = windowSize / 2;

        x1 = x - halfWindow - 1;
        y1 = y - halfWindow - 1;

        x2 = x + halfWindow;
        y2 = y + halfWindow;

        if (x1 < -1)
        {
            x1 = -1;
        }

        if (y1 < -1)
        {
            y1 = -1;
        }

        if (x2 >= width)
        {
            x2 = width - 1;
        }

        if (y2 >= height)
        {
            y2 = height - 1;
        }
    }

    private static unsafe void ComputeHorizontalRow(
        IntPtr sourcePtr,
        IntPtr sumPtr,
        IntPtr sumSqPtr,
        int y,
        int width,
        int stride,
        int satWidth)
    {
        byte* source = (byte*)sourcePtr.ToPointer();
        long* sum = (long*)sumPtr.ToPointer();
        long* sumSq = (long*)sumSqPtr.ToPointer();

        long rowSum = 0;
        long rowSumSq = 0;

        int sourceRow = y * stride;
        int satRow = y * satWidth;

        for (int x = 0; x < width; x++)
        {
            byte value = source[sourceRow + x];

            rowSum += value;
            rowSumSq += (long)value * value;

            sum[satRow + x] = rowSum;
            sumSq[satRow + x] = rowSumSq;
        }
    }
}