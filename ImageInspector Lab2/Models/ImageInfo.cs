namespace ImageInspector.Models
{
    public class ImageInfo
    {
        public string FileName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string Format { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public double DpiX { get; set; }
        public double DpiY { get; set; }
        public int BitDepth { get; set; }
        public string Compression { get; set; } = "";
        public string Extra { get; set; } = "";
        public string Status { get; set; } = "OK";
    }
}