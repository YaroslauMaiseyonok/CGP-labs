namespace Lab4Raster
{
    public interface IRasterTarget
    {
        int Width { get; }
        int Height { get; }

        void SetPixel(int x, int y, double intensity);
    }
}