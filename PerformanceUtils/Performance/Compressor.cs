using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;

namespace PerformanceUtils.Performance
{
    public static class Compressor
    {
        private static ConcurrentDictionary<string, byte[]> StringBytes = new ConcurrentDictionary<string, byte[]>();
        private static ConcurrentDictionary<byte[], string> BytesString = new ConcurrentDictionary<byte[], string>(new BytesEqualityComparer());

        public static byte[] Compress(string str)
        {
            if (!StringBytes.TryGetValue(str, out var result))
            {
                var bytes = Encoding.UTF8.GetBytes(str);
                using var output = new MemoryStream();
                using var dstream = new DeflateStream(output, CompressionLevel.Fastest);

                dstream.Write(bytes, 0, bytes.Length);
                dstream.Flush();
                result = output.ToArray();

                BytesString.TryAdd(result, str);
                StringBytes.TryAdd(str, result);
            }
            return result;
        }

        public static string Decompress(byte[] bytes)
        {

            if (!BytesString.TryGetValue(bytes, out var result))
            {
                using var input = new MemoryStream(bytes);
                using var output = new MemoryStream();
                using var dstream = new DeflateStream(input, CompressionMode.Decompress);
                dstream.CopyTo(output);
                result = Encoding.UTF8.GetString(output.ToArray());

                BytesString.TryAdd(bytes, result);
                StringBytes.TryAdd(result, bytes);
            }
            return result;
        }

        public static void Clear()
        {
            StringBytes.Clear();
            BytesString.Clear();
        }

        private class BytesEqualityComparer : ArrayEqualityComparer<byte>
        {
            public override int GetHashCode(byte[] obj)
            {
                int result = 17;
                for (int i = 0; i < obj.Length; i++)
                {
                    unchecked
                    {
                        result = result * 23 + obj[i];
                    }
                }
                return result;
            }

        }
    }
}
