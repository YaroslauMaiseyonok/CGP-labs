using System;
using System.IO;
using ImageInspector.Models;
using ImageInspector.Utils;

namespace ImageInspector.Parsers
{
    internal static class BmpParser
    {
        public static ImageInfo Parse(string path)
        {
            var info = new ImageInfo { FileName = Path.GetFileName(path), Format = "BMP" };

            using var fs = ByteUtils.OpenRead(path);
            if (fs.Length < 54)
            {
                info.Status = "Файл повреждён (размер < 54 байт)";
                return info;
            }

            var h = new byte[54];
            if (!ByteUtils.ReadFully(fs, h, 54))
            {
                info.Status = "Файл повреждён (не читается заголовок)";
                return info;
            }

            // BITMAPFILEHEADER
            if (h[0] != 0x42 || h[1] != 0x4D)
            {
                info.Status = "Не BMP (нет сигнатуры 'BM')";
                return info;
            }

            uint bfSize = ByteUtils.U32LE(h, 2);
            uint bfOffBits = ByteUtils.U32LE(h, 10);

            // BITMAPINFOHEADER
            uint biSize = ByteUtils.U32LE(h, 14);
            int biWidth = (int)ByteUtils.U32LE(h, 18);
            int biHeight = (int)ByteUtils.U32LE(h, 22);
            ushort biPlanes = ByteUtils.U16LE(h, 26);
            ushort biBitCount = ByteUtils.U16LE(h, 28);
            uint biCompression = ByteUtils.U32LE(h, 30);
            int biXPpm = (int)ByteUtils.U32LE(h, 38);
            int biYPpm = (int)ByteUtils.U32LE(h, 42);
            uint biClrUsed = ByteUtils.U32LE(h, 46);

            info.Width = biWidth;
            info.Height = Math.Abs(biHeight);
            info.BitDepth = biBitCount;
            info.DpiX = biXPpm > 0 ? biXPpm * 0.0254 : 72;
            info.DpiY = biYPpm > 0 ? biYPpm * 0.0254 : 72;

            info.Compression = biCompression switch
            {
                0 => "BI_RGB (без сжатия)",
                1 => "BI_RLE8",
                2 => "BI_RLE4",
                3 => "BI_BITFIELDS",
                4 => "BI_JPEG",
                5 => "BI_PNG",
                _ => $"Неизвестно ({biCompression})"
            };

            if (biBitCount <= 8)
            {
                int paletteEntries = biClrUsed > 0 ? (int)biClrUsed : (1 << biBitCount);
                info.Extra = $"Палитра: {paletteEntries} цветов; OffBits={bfOffBits}; biSize={biSize}";
            }
            else
            {
                info.Extra = $"OffBits={bfOffBits}; biSize={biSize}";
            }

            if (fs.Length < bfSize)
                info.Status = "Файл повреждён (неполный: длина < bfSize)";

            return info;
        }
    }
}