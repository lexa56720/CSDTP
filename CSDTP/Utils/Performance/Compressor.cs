using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Utils.Performance
{
    internal static class Compressor
    {
        private static ConcurrentDictionary<string, byte[]> StringBytes = new ConcurrentDictionary<string, byte[]>();
        private static ConcurrentDictionary<byte[], string> BytesString = new ConcurrentDictionary<byte[], string>(new ArrayEqualityComparer());

        public static byte[] Compress(string str)
        {
            if (!StringBytes.TryGetValue(str, out var result))
            {
                var bytes = Encoding.ASCII.GetBytes(str);
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
                result = Encoding.ASCII.GetString(output.ToArray());

                BytesString.TryAdd(bytes,result);
                StringBytes.TryAdd(result,bytes);
            }
            return result;
        }


        private class ArrayEqualityComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[]? x, byte[]? y)
            {
                if (x == null || y == null)
                    return false;

                if (x.Length != y.Length)
                    return false;

                for (int i = 0; i < x.Length; i++)
                    if (x[i] != y[i])
                        return false;

                return true;
            }

            public int GetHashCode(byte[] obj)
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
