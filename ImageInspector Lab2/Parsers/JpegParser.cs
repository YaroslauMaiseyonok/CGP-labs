using System;
using System.IO;
using ImageInspector.Models;
using ImageInspector.Utils;

namespace ImageInspector.Parsers
{
    internal static class JpegParser
    {
        public static ImageInfo Parse(string path)
        {
            var info = new ImageInfo { FileName = Path.GetFileName(path), Format = "JPEG" };

            using var fs = ByteUtils.OpenRead(path);

            var soi = new byte[2];
            if (!ByteUtils.ReadFully(fs, soi, 2) || soi[0] != 0xFF || soi[1] != 0xD8)
            {
                info.Status = "Не JPEG (нет SOI)";
                return info;
            }

            info.DpiX = 72; info.DpiY = 72;
            info.Compression = "JPEG (DCT)";
            bool sawSOF = false;

            while (fs.Position < fs.Length - 1)
            {
                int b = fs.ReadByte();
                if (b < 0) break;
                if (b != 0xFF) continue;

                int marker;
                do { marker = fs.ReadByte(); } while (marker == 0xFF);
                if (marker < 0) break;

                if (marker == 0xD8 || marker == 0xD9 ||
                    (marker >= 0xD0 && marker <= 0xD7) || marker == 0x01)
                    continue;

                var lenBuf = new byte[2];
                if (!ByteUtils.ReadFully(fs, lenBuf, 2)) break;
                int segLen = (lenBuf[0] << 8) | lenBuf[1];
                if (segLen < 2) { info.Status = "Файл повреждён (некорректная длина сегмента)"; return info; }
                int dataLen = segLen - 2;
                long dataStart = fs.Position;

                if (marker == 0xE0 && dataLen >= 12) // APP0 / JFIF
                {
                    var data = new byte[Math.Min(dataLen, 16)];
                    ByteUtils.ReadFully(fs, data, data.Length);
                    if (data[0] == 'J' && data[1] == 'F' && data[2] == 'I' && data[3] == 'F' && data[4] == 0)
                    {
                        int units = data[7];
                        int xd = (data[8] << 8) | data[9];
                        int yd = (data[10] << 8) | data[11];
                        if (units == 1) { info.DpiX = xd; info.DpiY = yd; }
                        else if (units == 2) { info.DpiX = xd * 2.54; info.DpiY = yd * 2.54; }
                    }
                    fs.Seek(dataStart + dataLen, SeekOrigin.Begin);
                }
                else if (marker == 0xC0 || marker == 0xC1 || marker == 0xC2 ||
                         marker == 0xC3 || marker == 0xC5 || marker == 0xC6 ||
                         marker == 0xC7 || marker == 0xC9 || marker == 0xCA ||
                         marker == 0xCB || marker == 0xCD || marker == 0xCE || marker == 0xCF)
                {
                    var data = new byte[Math.Min(dataLen, 8)];
                    ByteUtils.ReadFully(fs, data, data.Length);
                    if (data.Length >= 6)
                    {
                        info.BitDepth = data[0];
                        info.Height = (data[1] << 8) | data[2];
                        info.Width = (data[3] << 8) | data[4];
                        int ncomp = data[5];
                        info.Extra = $"Компонент: {ncomp}; тип кадра: 0x{marker:X2}";
                        sawSOF = true;
                    }
                    fs.Seek(dataStart + dataLen, SeekOrigin.Begin);
                }
                else if (marker == 0xDA)
                {
                    break;
                }
                else
                {
                    fs.Seek(dataStart + dataLen, SeekOrigin.Begin);
                }
            }

            if (fs.Length >= 2)
            {
                fs.Seek(-2, SeekOrigin.End);
                int e1 = fs.ReadByte();
                int e2 = fs.ReadByte();
                if (!(e1 == 0xFF && e2 == 0xD9))
                    info.Status = "Файл повреждён (нет маркера EOI)";
            }

            if (!sawSOF && info.Status == "OK")
                info.Status = "Файл повреждён (нет SOF)";

            return info;
        }
    }
}