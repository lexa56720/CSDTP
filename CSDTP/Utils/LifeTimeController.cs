using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Utils
{
    internal class LifeTimeController<T> where T : IDisposable
    {

        public List<KeyValuePair<T, DateTime>> Objects = new List<KeyValuePair<T, DateTime>>();

        private object locker = new object();

        private TimeSpan LifeTime { get; init; }

        public bool IsRunning { get; private set; }

        public LifeTimeController(TimeSpan lifeTime)
        {
            LifeTime = lifeTime;
        }

        public void Start()
        {
            if (IsRunning)
                return;

            IsRunning = true;
            Check();
        }
        public void Stop()
        {
            if (!IsRunning)
                return;

            IsRunning = false;
         
        }

        public void Clear()
        {
            for (int i = 0; i < Objects.Count; i++)
                Objects[i].Key.Dispose();
            Objects.Clear();
        }

        public void Add(T obj)
        {
            Objects.Add(new KeyValuePair<T, DateTime>(obj, DateTime.Now.Add(LifeTime)));
        }
        public T? Get(Predicate<T> predicate)
        {
            lock (locker)
            {
                for (int i = 0; i < Objects.Count; i++)
                {
                    if (predicate(Objects[i].Key))
                    {
                        Objects[i].Value.Add(LifeTime);
                        return Objects[i].Key;
                    }
                }
                return default(T);
            }
        }
        public void Check()
        {
            Task.Run(async () =>
            {
                while (IsRunning)
                {
                    var nowTime = DateTime.Now;
                    var newObjects = new List<KeyValuePair<T, DateTime>>();
                    lock (locker)
                    {
                        for (int i = 0; i < Objects.Count; i++)
                        {
                            if (Objects[i].Value < nowTime)
                                newObjects.Add(Objects[i]);
                            else
                                Objects[i].Key.Dispose();
                        }
                        Objects = newObjects;
                    }

                    await Task.Delay(LifeTime);
                }
            });

            Clear();
        }

    }
}
