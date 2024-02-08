using System.Collections.Concurrent;

namespace CSDTP.Utils.Collections
{
    internal class QueueProcessor<T>
    {
        private ConcurrentQueue<T> Queue = new ConcurrentQueue<T>();
        private Action<T> HandleItem;
        public int SequentialLimit { get; }
        public TimeSpan Timeout { get; }

        public bool IsRunning { get; private set; }

        public QueueProcessor(Action<T> handleItem, int seqentialLimit, TimeSpan timeout)
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
            HandleQueue();
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

        private void HandleQueue()
        {
            int count = 0;
            Task.Run(async () =>
            {
                while (IsRunning)
                {
                    count = Queue.Count;

                    if (count > 0)
                        ProcessSequentially(count);
                    else
                        await Task.Delay(Timeout);
                }
            });
        }

        private void ProcessSequentially(int count)
        {
            for (int i = 0; i < count; i++)
                if (Queue.TryDequeue(out var data))
                    HandleItem(data);
        }
        private void ProcessParallel(int count)
        {
            Task.Run(() =>
            {
                Parallel.For(0, count, (i) =>
                {
                    if (Queue.TryDequeue(out var data))
                        HandleItem(data);
                });
            });
        }
    }
}
