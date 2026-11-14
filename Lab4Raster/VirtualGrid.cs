using System;

namespace Lab4Raster
{
    public sealed class VirtualGrid : IRasterTarget
    {
        private readonly float[] _pixels;

        public VirtualGrid(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Размер виртуального растра должен быть положительным.");

            Width = width;
            Height = height;
            _pixels = new float[width * height];
        }

        public int Width { get; }

        public int Height { get; }

        public long SetPixelCalls { get; private set; }

        public long ChangedPixels { get; private set; }

        public float[] Pixels => _pixels;

        public void Clear()
        {
            Array.Clear(_pixels, 0, _pixels.Length);
            SetPixelCalls = 0;
            ChangedPixels = 0;
        }

        public void SetPixel(int x, int y, double intensity)
        {
            SetPixelCalls++;

            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
                return;

            if (intensity <= 0.0)
                return;

            if (intensity > 1.0)
                intensity = 1.0;

            int index = y * Width + x;
            float value = (float)intensity;

            if (value > _pixels[index])
            {
                if (_pixels[index] == 0f)
                    ChangedPixels++;

                _pixels[index] = value;
            }
        }

        public float GetPixel(int x, int y)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
                return 0f;

            return _pixels[y * Width + x];
        }
    }
}
