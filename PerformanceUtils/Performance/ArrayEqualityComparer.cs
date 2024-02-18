using System.Diagnostics.CodeAnalysis;

namespace PerformanceUtils.Performance
{
    internal abstract class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        public bool Equals(T[]? x, T[]? y)
        {
            if (x == null || y == null)
                return false;

            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; i++)
                if (!x[i].Equals(y[i]))
                    return false;

            return true;
        }

        public abstract int GetHashCode([DisallowNull] T[] obj);
    }
}
