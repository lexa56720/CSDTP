using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Utils.Collections
{
    internal class QueueProcessorAsync<T>
    {
        private ConcurrentQueue<(T item,DateTime addedTime)> Queue = new();
        private Func<T, Task> HandleItem;
        public int SequentialLimit { get; }
        public TimeSpan Timeout { get; }

        public bool IsRunning { get; private set; }

        public QueueProcessorAsync(Func<T, Task> handleItem, int sequentialLimit, TimeSpan timeout)
        {
            HandleItem = handleItem;
            SequentialLimit = sequentialLimit;
            Timeout = timeout;
        }

        public void Start()
        {
            if (IsRunning)
                return;

            IsRunning = true;
            Task.Run(HandleQueueAsync);
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            Queue.Clear();
            IsRunning = false;
        }

        public void Add(T item)
        {
            Queue.Enqueue((item,DateTime.UtcNow));
        }

        public void Clear()
        {
            Queue.Clear();
        }

        private async Task HandleQueueAsync()
        {
            while (IsRunning)
            {
                int count = Queue.Count;
                if (count > 0 && count < SequentialLimit)
                {
                    await Parallel.ForAsync(0, count, async (i, c) =>
                      {
                          if (Queue.TryDequeue(out var data))
                              await HandleItem(data.item);
                      });
                }
                else if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (Queue.TryDequeue(out var data))
                            await HandleItem(data.item);
                    }
                }
                else
                    await Task.Delay(Timeout);
            }
        }
    }
}
