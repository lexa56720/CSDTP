using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Utils.Performance
{
    internal class GlobalByteDictionary<T>
    {

        public static ConcurrentDictionary<byte[], T> Dictionary = new ConcurrentDictionary<byte[], T>();

        public T Get(byte[] key, Func<byte[],T> extractor)
        {
            if(!Dictionary.TryGetValue(key, out var result))
            {
                result= extractor(key);
                Dictionary.TryAdd(key, result);
            }
            return result;
        }
    }
}
