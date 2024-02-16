using System.Net;

namespace CSDTP.Protocols.Abstracts
{
    internal abstract class BaseReceiver : IReceiver
    {
        public virtual bool IsReceiving { get; protected set; }

        protected CancellationTokenSource? TokenSource { get; set; }

        public virtual int Port { get; }

        public event EventHandler<(IPAddress from, byte[] data)>? DataAppear;
        public BaseReceiver(int port)
        {
            Port = port;
        }
        public BaseReceiver()
        {
        }

        public virtual void Dispose()
        {
            Stop();
            if (TokenSource != null)
            {
                TokenSource.Cancel();
                TokenSource.Dispose();
            }
        }

        public virtual async ValueTask Start()
        {
            if (IsReceiving)
                return;

            IsReceiving = true;

            TokenSource = new CancellationTokenSource();
            var token = TokenSource.Token;

            ReceiveWork(token);
        }

        protected abstract Task ReceiveWork(CancellationToken token);

        public virtual async ValueTask Stop()
        {
            if (!IsReceiving)
                return;

            IsReceiving = false;
        }

        protected virtual void OnDataAppear(byte[] bytes, IPAddress ip)
        {
            DataAppear?.Invoke(this, (ip, bytes));
        }
    }
}
