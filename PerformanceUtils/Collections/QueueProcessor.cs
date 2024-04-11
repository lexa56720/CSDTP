using System.Collections.Concurrent;

namespace PerformanceUtils.Collections
{
    public class QueueProcessor<T>
    {
        private ConcurrentQueue<(T item, DateTime addedTime)> Queue = new();
        private Func<T, Task> HandleItem;
        public int SequentialLimit { get; }
        public TimeSpan Timeout { get; }

        public bool IsRunning { get; private set; }

        public QueueProcessor(Func<T, Task> handleItem, int sequentialLimit, TimeSpan timeout)
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
            Queue.Enqueue((item, DateTime.UtcNow));
        }

        public void Clear()
        {
            Queue.Clear();
        }

        private async Task HandleQueueAsync()
        {
            while (IsRunning)
            {
                var count = Queue.Count;
                for (int i = 0; i < count; i++)
                {
                    if (Queue.TryDequeue(out var data))
                        await HandleItem(data.item);
                }
            }
        }
    }
}
