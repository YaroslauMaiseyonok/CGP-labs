using System.IO;

namespace ImageInspector.Utils
{
    internal static class ByteUtils
    {
        public static ushort U16LE(byte[] b, int o) => (ushort)(b[o] | (b[o + 1] << 8));
        public static ushort U16BE(byte[] b, int o) => (ushort)((b[o] << 8) | b[o + 1]);

        public static uint U32LE(byte[] b, int o) =>
            (uint)(b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24));

        public static uint U32BE(byte[] b, int o) =>
            (uint)((b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3]);

        public static bool ReadFully(Stream s, byte[] buf, int count)
        {
            int total = 0;
            while (total < count)
            {
                int n = s.Read(buf, total, count - total);
                if (n <= 0) return false;
                total += n;
            }
            return true;
        }

        public static FileStream OpenRead(string path) =>
            new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                           bufferSize: 4096, FileOptions.SequentialScan);
    }
}