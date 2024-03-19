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

        private readonly ConcurrentDictionary<TKey, TValue> Values = new();

        private readonly ConcurrentDictionary<TKey, CustomTimer> Timers = new();

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
            if (key == null || Values.ContainsKey(key))
                return false;

            var result = Values.TryAdd(key, value);

            if (lifetime != TimeSpan.MaxValue)
            {
                var timer = CreateTimer(lifetime, key);
                Timers.TryAdd(key, timer);
                timer.Start();
            }
            return result;
        }
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key != null && Values.TryGetValue(key, out value))
                return true;

            value = default;
            return false;
        }
        public bool UpdateLifetime(TKey key, TimeSpan lifetime)
        {
            if (!Values.ContainsKey(key) || !Timers.TryRemove(key, out var timer))
                return false;

            DisposeTimer(timer);


            if (lifetime != TimeSpan.MaxValue)
            {
                timer = CreateTimer(lifetime, key);
                Timers.TryAdd(key, timer);
                timer.Start();
            }
           
            return true;
        }
        public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue result)
        {
            if (Timers.TryRemove(key, out var timer))
                DisposeTimer(timer);
            return Values.TryRemove(key, out result);
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
            Values.Clear();
            foreach (var timer in Timers.Values)
                DisposeTimer(timer);
            Timers.Clear();
        }
    }
}
