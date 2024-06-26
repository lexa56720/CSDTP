﻿using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace PerformanceUtils.Performance
{
    public class GlobalByteDictionary<T> where T : class
    {

        public static ConcurrentDictionary<byte[], T> Dictionary = new ConcurrentDictionary<byte[], T>(new ByteArrayComparer());

        public static T? Get(byte[] key, Func<byte[], T?> extractor)
        {
            if (!Dictionary.TryGetValue(key, out var result))
            {
                try
                {
                    result = extractor(key);
                    if (result != null)
                        Dictionary.TryAdd(key, result);
                }
                catch
                {
                    return null;
                }
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
