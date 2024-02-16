using System.Net;

namespace CSDTP.Protocols.Abstracts
{
    public interface IReceiver : IDisposable
    {
        public event EventHandler<(IPAddress from, byte[] data)> DataAppear;
        public bool IsReceiving { get; }
        public int Port { get; }

        public ValueTask Start();
        public ValueTask Stop();
    }
}
