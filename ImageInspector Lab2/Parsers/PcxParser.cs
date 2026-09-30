using System.IO;
using ImageInspector.Models;
using ImageInspector.Utils;

namespace ImageInspector.Parsers
{
    internal static class PcxParser
    {
        public static ImageInfo Parse(string path)
        {
            var info = new ImageInfo { FileName = Path.GetFileName(path), Format = "PCX" };

            using var fs = ByteUtils.OpenRead(path);

            var h = new byte[128];
            if (!ByteUtils.ReadFully(fs, h, 128))
            {
                info.Status = "Файл повреждён (заголовок < 128 байт)";
                return info;
            }

            if (h[0] != 0x0A)
            {
                info.Status = "Не PCX (manufacturer != 0x0A)";
                return info;
            }

            byte version = h[1];
            byte encoding = h[2];
            byte bitsPerPixel = h[3];

            ushort xmin = ByteUtils.U16LE(h, 4);
            ushort ymin = ByteUtils.U16LE(h, 6);
            ushort xmax = ByteUtils.U16LE(h, 8);
            ushort ymax = ByteUtils.U16LE(h, 10);
            ushort hdpi = ByteUtils.U16LE(h, 12);
            ushort vdpi = ByteUtils.U16LE(h, 14);
            byte nplanes = h[65];
            ushort bytesPerLine = ByteUtils.U16LE(h, 66);

            info.Width = xmax - xmin + 1;
            info.Height = ymax - ymin + 1;
            info.BitDepth = bitsPerPixel * nplanes;
            info.DpiX = hdpi > 0 ? hdpi : 72;
            info.DpiY = vdpi > 0 ? vdpi : 72;
            info.Compression = encoding == 1 ? "RLE" : "Без сжатия";
            info.Extra = $"Версия: {version}; Плоскостей: {nplanes}; Байт/строка: {bytesPerLine}";

            return info;
        }
    }
}