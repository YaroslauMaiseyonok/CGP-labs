using System.IO;
using System.Text;
using ImageInspector.Models;
using ImageInspector.Utils;

namespace ImageInspector.Parsers
{
    internal static class GifParser
    {
        public static ImageInfo Parse(string path)
        {
            var info = new ImageInfo { FileName = Path.GetFileName(path), Format = "GIF" };

            using var fs = ByteUtils.OpenRead(path);

            var h = new byte[13];
            if (!ByteUtils.ReadFully(fs, h, 13))
            {
                info.Status = "Файл повреждён";
                return info;
            }

            string sig = Encoding.ASCII.GetString(h, 0, 6);
            if (sig != "GIF87a" && sig != "GIF89a")
            {
                info.Status = "Не GIF";
                return info;
            }

            info.Width = ByteUtils.U16LE(h, 6);
            info.Height = ByteUtils.U16LE(h, 8);

            byte flags = h[10];
            bool hasGCT = (flags & 0x80) != 0;
            int colorBits = (flags & 0x07) + 1;
            int colorCount = 1 << colorBits;

            info.BitDepth = colorBits;
            info.DpiX = 72;
            info.DpiY = 72;
            info.Compression = "LZW";

            info.Extra = hasGCT
                ? $"Глобальная палитра: {colorCount} цветов"
                : "Глобальная палитра отсутствует";

            if (fs.Length >= 1)
            {
                fs.Seek(-1, SeekOrigin.End);
                int last = fs.ReadByte();
                if (last != 0x3B)
                    info.Status = "Файл повреждён (нет 0x3B)";
            }

            return info;
        }
    }
}