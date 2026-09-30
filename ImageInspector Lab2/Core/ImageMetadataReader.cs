using System;
using System.IO;
using ImageInspector.Models;
using ImageInspector.Parsers;
using ImageInspector.Utils;

namespace ImageInspector.Core
{
    public static class ImageMetadataReader
    {

        public static ImageInfo ReadMetadata(string path)
        {
            var info = ReadMetadataInfo(path);
            info.FullPath = path;
            if (string.IsNullOrEmpty(info.FileName))
                info.FileName = Path.GetFileName(path);
            return info;
        }
        private static ImageInfo ReadMetadataInfo(string path)
        {
            try
            {
                var head = new byte[16];
                int n;
                using (var fs = ByteUtils.OpenRead(path))
                {
                    n = fs.Read(head, 0, 16);
                }

                if (n < 2)
                    return new ImageInfo
                    {
                        FileName = Path.GetFileName(path),
                        Status = "Файл повреждён (слишком мал)"
                    };

                // BMP "BM"
                if (n >= 2 && head[0] == 0x42 && head[1] == 0x4D)
                    return BmpParser.Parse(path);

                // PNG 89 50 4E 47 0D 0A 1A 0A
                if (n >= 8 && head[0] == 0x89 && head[1] == 0x50 &&
                    head[2] == 0x4E && head[3] == 0x47 &&
                    head[4] == 0x0D && head[5] == 0x0A &&
                    head[6] == 0x1A && head[7] == 0x0A)
                    return PngParser.Parse(path);

                // JPEG FF D8
                if (n >= 2 && head[0] == 0xFF && head[1] == 0xD8)
                    return JpegParser.Parse(path);

                // GIF87a / GIF89a
                if (n >= 6 && head[0] == 'G' && head[1] == 'I' && head[2] == 'F')
                    return GifParser.Parse(path);

                // TIFF II*\0 или MM\0*
                if (n >= 4 &&
                    ((head[0] == 0x49 && head[1] == 0x49 && head[2] == 0x2A && head[3] == 0x00) ||
                     (head[0] == 0x4D && head[1] == 0x4D && head[2] == 0x00 && head[3] == 0x2A)))
                    return TiffParser.Parse(path);

                // PCX 0x0A
                if (head[0] == 0x0A)
                    return PcxParser.Parse(path);

                return new ImageInfo
                {
                    FileName = Path.GetFileName(path),
                    Status = "Неизвестный формат (нет совпадений с сигнатурами)"
                };
            }
            catch (Exception ex)
            {
                return new ImageInfo
                {
                    FileName = Path.GetFileName(path),
                    Status = "Ошибка: " + ex.Message
                };
            }
        }
    }
}