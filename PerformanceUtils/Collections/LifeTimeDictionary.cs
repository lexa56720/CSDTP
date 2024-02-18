using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Timers;

namespace PerformanceUtils.Collections
{


    public class LifeTimeDictionary<TKey, TValue> where TKey : notnull
    {
        private class CustomTimer : System.Timers.Timer
        {
            public CustomTimer()
            {
            }

            public CustomTimer(double interval) : base(interval)
            {
            }

            public CustomTimer(TimeSpan interval) : base(interval)
            {
            }

            public object? Obj { get; set; }
        }

        private ConcurrentDictionary<TKey, TValue> dict = new();

        private ConcurrentDictionary<TKey, CustomTimer> timers = new();

        public LifeTimeDictionary(Action<TValue?> itemRemoved)
        {
            RemoveCallback = (keyObj, e) =>
            {
                TryRemove((TKey)((CustomTimer)keyObj).Obj, out var result);
                itemRemoved(result);
            };
        }

        public LifeTimeDictionary()
        {
            RemoveCallback = (keyObj, e) =>
            {
                TryRemove((TKey)((CustomTimer)keyObj).Obj, out var result);
            };
        }


        private readonly ElapsedEventHandler RemoveCallback;

        public bool TryAdd(TKey key, TValue value, TimeSpan lifetime)
        {
            if (key == null || dict.ContainsKey(key))
                return false;

            var result = dict.TryAdd(key, value);

            var timer = CreateTimer(lifetime, key);
            timers.TryAdd(key, timer);
            timer.Start();

            return result;
        }
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key != null && dict.TryGetValue(key, out value))
                return true;

            value = default;
            return false;
        }
        public bool UpdateLifetime(TKey key, TimeSpan lifetime)
        {
            if (!dict.ContainsKey(key) || !timers.TryRemove(key, out var timer))
                return false;

            DisposeTimer(timer);

            timer = CreateTimer(lifetime, key);
            timers.TryAdd(key, timer);
            timer.Start();

            return true;
        }
        public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue result)
        {
            if (timers.TryRemove(key, out var timer))
                DisposeTimer(timer);
            return dict.TryRemove(key, out result);
        }

        private void DisposeTimer(CustomTimer timer)
        {
            timer.Elapsed -= RemoveCallback;
            timer.Stop();
            timer.Dispose();
        }
        private CustomTimer CreateTimer(TimeSpan period, TKey key)
        {
            var timer = new CustomTimer(period);

            timer.Elapsed += RemoveCallback;
            timer.AutoReset = false;
            timer.Obj = key;
            return timer;
        }

        public void Clear()
        {
            dict.Clear();
            foreach (var timer in timers.Values)
                DisposeTimer(timer);
            timers.Clear();
        }
    }
}
