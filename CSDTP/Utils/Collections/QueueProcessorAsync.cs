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
        private ConcurrentQueue<T> Queue = new ConcurrentQueue<T>();
        private Func<T, Task> HandleItem;
        public int SequentialLimit { get; }
        public TimeSpan Timeout { get; }

        public bool IsRunning { get; private set; }

        public QueueProcessorAsync(Func<T, Task> handleItem, int seqentialLimit, TimeSpan timeout)
        {
            HandleItem = handleItem;
            SequentialLimit = seqentialLimit;
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
            Queue.Enqueue(item);
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
                if (count > 0)
                {
                    var tasks = new Task[count];
                    for (int i = 0; i < count; i++)
                        if (Queue.TryDequeue(out var data))
                            tasks[i] = HandleItem(data);

                    await Task.WhenAll(tasks);
                }
                else
                    await Task.Delay(Timeout);
            }
        }
    }
}
