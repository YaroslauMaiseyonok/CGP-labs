using System;
using System.IO;
using ImageInspector.Models;
using ImageInspector.Utils;

namespace ImageInspector.Parsers
{
    internal static class TiffParser
    {
        public static ImageInfo Parse(string path)
        {
            var info = new ImageInfo { FileName = Path.GetFileName(path), Format = "TIFF" };

            using var fs = ByteUtils.OpenRead(path);

            var hdr = new byte[8];
            if (!ByteUtils.ReadFully(fs, hdr, 8))
            {
                info.Status = "Файл повреждён (нет заголовка)";
                return info;
            }

            bool le;
            if (hdr[0] == 0x49 && hdr[1] == 0x49) le = true;
            else if (hdr[0] == 0x4D && hdr[1] == 0x4D) le = false;
            else { info.Status = "Не TIFF"; return info; }

            ushort magic = le ? ByteUtils.U16LE(hdr, 2) : ByteUtils.U16BE(hdr, 2);
            if (magic != 42) { info.Status = "Не TIFF (magic != 42)"; return info; }

            uint ifdOffset = le ? ByteUtils.U32LE(hdr, 4) : ByteUtils.U32BE(hdr, 4);
            if (ifdOffset == 0 || ifdOffset >= fs.Length)
            {
                info.Status = "Файл повреждён (некорректный IFD offset)";
                return info;
            }

            fs.Seek(ifdOffset, SeekOrigin.Begin);
            var cnt = new byte[2];
            if (!ByteUtils.ReadFully(fs, cnt, 2))
            {
                info.Status = "Файл повреждён (нет IFD count)";
                return info;
            }

            ushort tagCount = le ? ByteUtils.U16LE(cnt, 0) : ByteUtils.U16BE(cnt, 0);
            info.DpiX = 72; info.DpiY = 72;
            int resUnit = 2;

            var tagBuf = new byte[12];
            for (int i = 0; i < tagCount; i++)
            {
                if (!ByteUtils.ReadFully(fs, tagBuf, 12)) break;

                ushort tag = le ? ByteUtils.U16LE(tagBuf, 0) : ByteUtils.U16BE(tagBuf, 0);
                ushort type = le ? ByteUtils.U16LE(tagBuf, 2) : ByteUtils.U16BE(tagBuf, 2);
                uint count = le ? ByteUtils.U32LE(tagBuf, 4) : ByteUtils.U32BE(tagBuf, 4);

                switch (tag)
                {
                    case 256: // ImageWidth
                        info.Width = (int)ReadInt(tagBuf, type, le);
                        break;
                    case 257: // ImageLength
                        info.Height = (int)ReadInt(tagBuf, type, le);
                        break;
                    case 258: // BitsPerSample
                        info.BitDepth = (int)ReadInt(tagBuf, type, le);
                        break;
                    case 259: // Compression
                        {
                            uint comp = (uint)ReadInt(tagBuf, type, le);
                            info.Compression = comp switch
                            {
                                1 => "Без сжатия",
                                2 => "CCITT 1D",
                                3 => "CCITT Group 3",
                                4 => "CCITT Group 4",
                                5 => "LZW",
                                6 => "JPEG (old)",
                                7 => "JPEG",
                                8 => "Deflate",
                                32773 => "PackBits",
                                _ => $"Неизвестно ({comp})"
                            };
                            break;
                        }
                    case 282: // XResolution (RATIONAL)
                        info.DpiX = ReadRational(fs, tagBuf, le);
                        break;
                    case 283: // YResolution
                        info.DpiY = ReadRational(fs, tagBuf, le);
                        break;
                    case 296: // ResolutionUnit
                        resUnit = (int)ReadInt(tagBuf, type, le);
                        break;
                }
            }

            if (resUnit == 3)
            {
                info.DpiX *= 2.54;
                info.DpiY *= 2.54;
            }

            info.Extra = $"Порядок байт: {(le ? "little-endian" : "big-endian")}; тегов в IFD: {tagCount}";
            return info;
        }

        private static long ReadInt(byte[] tagBuf, ushort type, bool le)
        {
            if (type == 3) // SHORT
                return le ? ByteUtils.U16LE(tagBuf, 8) : ByteUtils.U16BE(tagBuf, 8);
            if (type == 4) // LONG
                return le ? ByteUtils.U32LE(tagBuf, 8) : ByteUtils.U32BE(tagBuf, 8);
            if (type == 1) // BYTE
                return tagBuf[8];
            return 0;
        }

        private static double ReadRational(FileStream fs, byte[] tagBuf, bool le)
        {
            uint offset = le ? ByteUtils.U32LE(tagBuf, 8) : ByteUtils.U32BE(tagBuf, 8);
            long save = fs.Position;
            try
            {
                if (offset + 8 > fs.Length) return 0;
                fs.Seek(offset, SeekOrigin.Begin);
                var buf = new byte[8];
                if (!ByteUtils.ReadFully(fs, buf, 8)) return 0;
                uint num = le ? ByteUtils.U32LE(buf, 0) : ByteUtils.U32BE(buf, 0);
                uint den = le ? ByteUtils.U32LE(buf, 4) : ByteUtils.U32BE(buf, 4);
                return den == 0 ? 0 : (double)num / den;
            }
            finally
            {
                fs.Seek(save, SeekOrigin.Begin);
            }
        }
    }
}