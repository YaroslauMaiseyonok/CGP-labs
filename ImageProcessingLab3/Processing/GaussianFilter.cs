using System;
using System.Threading.Tasks;
using ImageProcessingLab3.Common;
using ImageProcessingLab3.Imaging;

namespace ImageProcessingLab3.Processing;
public static class GaussianFilter
{
    public static ImageBuffer Apply(ImageBuffer source, float sigma, EdgePadding padding)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (sigma <= 0f)
        {
            return source.Clone();
        }

        int radius = Math.Max(1, (int)Math.Ceiling(sigma * 3f));
        float[] kernel = BuildKernel(radius, sigma);

        var temp = new ImageBuffer(source.Width, source.Height);
        var result = new ImageBuffer(source.Width, source.Height);

        unsafe
        {
            fixed (byte* pSource = source.Data)
            fixed (byte* pTemp = temp.Data)
            fixed (byte* pResult = result.Data)
            fixed (float* pKernel = kernel)
            {
                IntPtr sourcePtr = (IntPtr)(void*)pSource;
                IntPtr tempPtr = (IntPtr)(void*)pTemp;
                IntPtr resultPtr = (IntPtr)(void*)pResult;
                IntPtr kernelPtr = (IntPtr)(void*)pKernel;

                Parallel.For(0, source.Height, y =>
                {
                    HorizontalRow(
                        sourcePtr,
                        tempPtr,
                        kernelPtr,
                        y,
                        source.Width,
                        source.Stride,
                        radius,
                        padding);
                });

                Parallel.For(0, source.Height, y =>
                {
                    VerticalRow(
                        tempPtr,
                        resultPtr,
                        kernelPtr,
                        y,
                        source.Width,
                        source.Height,
                        source.Stride,
                        radius,
                        padding);
                });
            }
        }

        return result;
    }

    private static float[] BuildKernel(int radius, float sigma)
    {
        int size = radius * 2 + 1;

        var kernel = new float[size];
        float sum = 0f;
        float denominator = 2f * sigma * sigma;

        for (int i = 0; i < size; i++)
        {
            float x = i - radius;
            kernel[i] = (float)Math.Exp(-(x * x) / denominator);
            sum += kernel[i];
        }

        for (int i = 0; i < size; i++)
        {
            kernel[i] /= sum;
        }

        return kernel;
    }

    private static unsafe void HorizontalRow(
        IntPtr sourcePtr,
        IntPtr tempPtr,
        IntPtr kernelPtr,
        int y,
        int width,
        int stride,
        int radius,
        EdgePadding padding)
    {
        byte* source = (byte*)sourcePtr.ToPointer();
        byte* temp = (byte*)tempPtr.ToPointer();
        float* kernel = (float*)kernelPtr.ToPointer();

        int sourceRow = y * stride;
        int tempRow = y * stride;

        for (int x = 0; x < width; x++)
        {
            float accumulator = 0f;

            for (int k = -radius; k <= radius; k++)
            {
                int sourceX = EdgeCoordinates.Resolve(x + k, width, padding);
                accumulator += source[sourceRow + sourceX] * kernel[k + radius];
            }

            temp[tempRow + x] = (byte)(accumulator + 0.5f);
        }
    }

    private static unsafe void VerticalRow(
        IntPtr tempPtr,
        IntPtr resultPtr,
        IntPtr kernelPtr,
        int y,
        int width,
        int height,
        int stride,
        int radius,
        EdgePadding padding)
    {
        byte* temp = (byte*)tempPtr.ToPointer();
        byte* result = (byte*)resultPtr.ToPointer();
        float* kernel = (float*)kernelPtr.ToPointer();

        int resultRow = y * stride;

        for (int x = 0; x < width; x++)
        {
            float accumulator = 0f;

            for (int k = -radius; k <= radius; k++)
            {
                int sourceY = EdgeCoordinates.Resolve(y + k, height, padding);
                accumulator += temp[sourceY * stride + x] * kernel[k + radius];
            }

            result[resultRow + x] = (byte)(accumulator + 0.5f);
        }
    }
}