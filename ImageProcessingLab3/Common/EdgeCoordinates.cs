using System.Runtime.CompilerServices;

namespace ImageProcessingLab3.Common;
public static class EdgeCoordinates
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Resolve(int coordinate, int length, EdgePadding padding)
    {
        return padding == EdgePadding.Clamp
            ? Clamp(coordinate, length)
            : Reflect(coordinate, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int coordinate, int length)
    {
        if (length <= 0)
        {
            return 0;
        }

        if (coordinate < 0)
        {
            return 0;
        }

        if (coordinate >= length)
        {
            return length - 1;
        }

        return coordinate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Reflect(int coordinate, int length)
    {
        if (length <= 1)
        {
            return 0;
        }

        int period = length * 2;

        coordinate %= period;

        if (coordinate < 0)
        {
            coordinate += period;
        }

        if (coordinate >= length)
        {
            coordinate = period - coordinate - 1;
        }

        return coordinate;
    }
}