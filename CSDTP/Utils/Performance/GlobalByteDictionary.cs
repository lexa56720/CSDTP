using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Utils.Performance
{
    internal class GlobalByteDictionary<T>
    {

        public static ConcurrentDictionary<byte[], T> Dictionary = new ConcurrentDictionary<byte[], T>(new ByteArrayComparer());

        public T Get(byte[] key, Func<byte[],T> extractor)
        {
            if(!Dictionary.TryGetValue(key, out var result))
            {
                result= extractor(key);
                Dictionary.TryAdd(key, result);
            }
            return result;
        }


        internal class ByteArrayComparer : ArrayEqualityComparer<byte>
        {
            public override int GetHashCode([DisallowNull] byte[] obj)
            {
                int result = 17;
                for (int i = 0; i < obj.Length; i++)
                {
                    unchecked
                    {
                        result = result * 23 + obj[i].GetHashCode();
                    }
                }
                return result;
            }
        }
    }
}
