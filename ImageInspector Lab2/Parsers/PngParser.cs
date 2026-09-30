using System.IO;
using System.Text;
using ImageInspector.Models;
using ImageInspector.Utils;

namespace ImageInspector.Parsers
{
    internal static class PngParser
    {
        private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public static ImageInfo Parse(string path)
        {
            var info = new ImageInfo { FileName = Path.GetFileName(path), Format = "PNG" };

            using var fs = ByteUtils.OpenRead(path);

            var sig = new byte[8];
            if (!ByteUtils.ReadFully(fs, sig, 8))
            {
                info.Status = "Файл повреждён (нет сигнатуры)";
                return info;
            }
            for (int i = 0; i < 8; i++)
            {
                if (sig[i] != Signature[i])
                {
                    info.Status = "Не PNG";
                    return info;
                }
            }

            bool sawIHDR = false, sawIEND = false;
            info.DpiX = 72; info.DpiY = 72;

            while (fs.Position + 8 <= fs.Length)
            {
                var ch = new byte[8];
                if (!ByteUtils.ReadFully(fs, ch, 8)) break;

                uint length = ByteUtils.U32BE(ch, 0);
                string type = Encoding.ASCII.GetString(ch, 4, 4);

                if (length > fs.Length - fs.Position - 4)
                {
                    info.Status = "Файл повреждён (длина чанка превышает размер файла)";
                    return info;
                }

                if (type == "IHDR")
                {
                    var data = new byte[13];
                    if (!ByteUtils.ReadFully(fs, data, 13))
                    {
                        info.Status = "Файл повреждён (IHDR)";
                        return info;
                    }
                    info.Width = (int)ByteUtils.U32BE(data, 0);
                    info.Height = (int)ByteUtils.U32BE(data, 4);
                    info.BitDepth = data[8];
                    int colorType = data[9];
                    info.Compression = $"Deflate; фильтр {data[11]}; интерлейс {data[12]}";
                    info.Extra = "Тип цвета: " + colorType switch
                    {
                        0 => "Grayscale",
                        2 => "RGB",
                        3 => "Indexed",
                        4 => "Grayscale + Alpha",
                        6 => "RGBA",
                        _ => "Unknown"
                    };
                    sawIHDR = true;

                    fs.Seek(length - 13 + 4, SeekOrigin.Current);
                }
                else if (type == "IEND")
                {
                    sawIEND = true;
                    break;
                }
                else
                {
                    fs.Seek(length + 4, SeekOrigin.Current);
                }
            }

            if (!sawIHDR) info.Status = "Файл повреждён (нет IHDR)";
            else if (!sawIEND) info.Status = "Файл повреждён (нет IEND)";

            return info;
        }
    }
}