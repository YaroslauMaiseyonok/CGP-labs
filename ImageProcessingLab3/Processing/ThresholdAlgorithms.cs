using ImageProcessingLab3.Imaging;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ImageProcessingLab3.Processing;
public static class ThresholdAlgorithms
{
    public static ImageBuffer Niblack(
        ImageBuffer source,
        IntegralImage integral,
        int windowSize,
        float k)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (integral == null)
        {
            throw new ArgumentNullException(nameof(integral));
        }

        windowSize = NormalizeWindow(windowSize);

        var result = new ImageBuffer(source.Width, source.Height);

        unsafe
        {
            fixed (byte* pSource = source.Data)
            fixed (byte* pResult = result.Data)
            fixed (long* pSum = integral.Sum)
            fixed (long* pSumSq = integral.SumSq)
            {
                IntPtr sourcePtr = (IntPtr)(void*)pSource;
                IntPtr resultPtr = (IntPtr)(void*)pResult;
                IntPtr sumPtr = (IntPtr)(void*)pSum;
                IntPtr sumSqPtr = (IntPtr)(void*)pSumSq;

                int ws = windowSize;

                Parallel.For(0, source.Height, y =>
                {
                    NiblackRow(
                        sourcePtr,
                        resultPtr,
                        sumPtr,
                        sumSqPtr,
                        y,
                        source.Width,
                        source.Height,
                        source.Stride,
                        integral.Width,
                        ws,
                        k);
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Савуола
    /// T = mean * (1 + k * (stdDev / R - 1))
    /// </summary>
    public static ImageBuffer Sauvola(
        ImageBuffer source,
        IntegralImage integral,
        int windowSize,
        float k,
        float r)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (integral == null)
        {
            throw new ArgumentNullException(nameof(integral));
        }

        if (r <= 0f)
        {
            r = 128f;
        }

        windowSize = NormalizeWindow(windowSize);

        var result = new ImageBuffer(source.Width, source.Height);

        unsafe
        {
            fixed (byte* pSource = source.Data)
            fixed (byte* pResult = result.Data)
            fixed (long* pSum = integral.Sum)
            fixed (long* pSumSq = integral.SumSq)
            {
                IntPtr sourcePtr = (IntPtr)(void*)pSource;
                IntPtr resultPtr = (IntPtr)(void*)pResult;
                IntPtr sumPtr = (IntPtr)(void*)pSum;
                IntPtr sumSqPtr = (IntPtr)(void*)pSumSq;

                int ws = windowSize;
                float savedR = r;

                Parallel.For(0, source.Height, y =>
                {
                    SauvolaRow(
                        sourcePtr,
                        resultPtr,
                        sumPtr,
                        sumSqPtr,
                        y,
                        source.Width,
                        source.Height,
                        source.Stride,
                        integral.Width,
                        ws,
                        k,
                        savedR);
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Брэдли
    /// T = mean * (1 - delta), где delta ~ 0.10...0.20.
    /// </summary>
    public static ImageBuffer Bradley(
        ImageBuffer source,
        IntegralImage integral,
        int windowSize,
        float delta)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (integral == null)
        {
            throw new ArgumentNullException(nameof(integral));
        }

        windowSize = NormalizeWindow(windowSize);

        var result = new ImageBuffer(source.Width, source.Height);

        unsafe
        {
            fixed (byte* pSource = source.Data)
            fixed (byte* pResult = result.Data)
            fixed (long* pSum = integral.Sum)
            {
                IntPtr sourcePtr = (IntPtr)(void*)pSource;
                IntPtr resultPtr = (IntPtr)(void*)pResult;
                IntPtr sumPtr = (IntPtr)(void*)pSum;

                int ws = windowSize;

                Parallel.For(0, source.Height, y =>
                {
                    BradleyRow(
                        sourcePtr,
                        resultPtr,
                        sumPtr,
                        y,
                        source.Width,
                        source.Height,
                        source.Stride,
                        integral.Width,
                        ws,
                        delta);
                });
            }
        }

        return result;
    }

    private static int NormalizeWindow(int windowSize)
    {
        if (windowSize < 3)
        {
            return 3;
        }

        return windowSize | 1;
    }

    private static unsafe void NiblackRow(
        IntPtr sourcePtr,
        IntPtr resultPtr,
        IntPtr sumPtr,
        IntPtr sumSqPtr,
        int y,
        int width,
        int height,
        int stride,
        int satWidth,
        int windowSize,
        float k)
    {
        byte* source = (byte*)sourcePtr.ToPointer();
        byte* result = (byte*)resultPtr.ToPointer();
        long* sum = (long*)sumPtr.ToPointer();
        long* sumSq = (long*)sumSqPtr.ToPointer();

        int row = y * stride;

        for (int x = 0; x < width; x++)
        {
            IntegralImage.GetWindowCoordinates(
                x,
                y,
                windowSize,
                width,
                height,
                out int x1,
                out int y1,
                out int x2,
                out int y2);

            long area = (long)(x2 - x1) * (y2 - y1);

            long rectSum =
                GetSatValue(sum, x2, y2, satWidth)
                - GetSatValue(sum, x1, y2, satWidth)
                - GetSatValue(sum, x2, y1, satWidth)
                + GetSatValue(sum, x1, y1, satWidth);

            long rectSumSq =
                GetSatValue(sumSq, x2, y2, satWidth)
                - GetSatValue(sumSq, x1, y2, satWidth)
                - GetSatValue(sumSq, x2, y1, satWidth)
                + GetSatValue(sumSq, x1, y1, satWidth);

            float mean = (float)rectSum / area;
            float variance = (float)rectSumSq / area - mean * mean;
            float stdDev = MathF.Sqrt(Math.Max(0f, variance));

            float threshold = mean + k * stdDev;

            result[row + x] = source[row + x] < threshold ? (byte)0 : (byte)255;
        }
    }

    private static unsafe void SauvolaRow(
        IntPtr sourcePtr,
        IntPtr resultPtr,
        IntPtr sumPtr,
        IntPtr sumSqPtr,
        int y,
        int width,
        int height,
        int stride,
        int satWidth,
        int windowSize,
        float k,
        float r)
    {
        byte* source = (byte*)sourcePtr.ToPointer();
        byte* result = (byte*)resultPtr.ToPointer();
        long* sum = (long*)sumPtr.ToPointer();
        long* sumSq = (long*)sumSqPtr.ToPointer();

        int row = y * stride;

        for (int x = 0; x < width; x++)
        {
            IntegralImage.GetWindowCoordinates(
                x,
                y,
                windowSize,
                width,
                height,
                out int x1,
                out int y1,
                out int x2,
                out int y2);

            long area = (long)(x2 - x1) * (y2 - y1);

            long rectSum =
                GetSatValue(sum, x2, y2, satWidth)
                - GetSatValue(sum, x1, y2, satWidth)
                - GetSatValue(sum, x2, y1, satWidth)
                + GetSatValue(sum, x1, y1, satWidth);

            long rectSumSq =
                GetSatValue(sumSq, x2, y2, satWidth)
                - GetSatValue(sumSq, x1, y2, satWidth)
                - GetSatValue(sumSq, x2, y1, satWidth)
                + GetSatValue(sumSq, x1, y1, satWidth);

            float mean = (float)rectSum / area;
            float variance = (float)rectSumSq / area - mean * mean;
            float stdDev = MathF.Sqrt(Math.Max(0f, variance));

            float threshold = mean * (1f + k * (stdDev / r - 1f));

            result[row + x] = source[row + x] < threshold ? (byte)0 : (byte)255;
        }
    }

    private static unsafe void BradleyRow(
        IntPtr sourcePtr,
        IntPtr resultPtr,
        IntPtr sumPtr,
        int y,
        int width,
        int height,
        int stride,
        int satWidth,
        int windowSize,
        float delta)
    {
        byte* source = (byte*)sourcePtr.ToPointer();
        byte* result = (byte*)resultPtr.ToPointer();
        long* sum = (long*)sumPtr.ToPointer();

        int row = y * stride;

        for (int x = 0; x < width; x++)
        {
            IntegralImage.GetWindowCoordinates(
                x,
                y,
                windowSize,
                width,
                height,
                out int x1,
                out int y1,
                out int x2,
                out int y2);

            long area = (long)(x2 - x1) * (y2 - y1);

            long rectSum =
                GetSatValue(sum, x2, y2, satWidth)
                - GetSatValue(sum, x1, y2, satWidth)
                - GetSatValue(sum, x2, y1, satWidth)
                + GetSatValue(sum, x1, y1, satWidth);

            float mean = (float)rectSum / area;

            float threshold = mean * (1f - delta);

            result[row + x] = source[row + x] < threshold ? (byte)0 : (byte)255;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe long GetSatValue(long* sat, int x, int y, int width)
    {
        if (x < 0 || y < 0)
        {
            return 0L;
        }

        return sat[y * width + x];
    }
}