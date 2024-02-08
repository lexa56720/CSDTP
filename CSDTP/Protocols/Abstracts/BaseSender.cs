using System.Net;

namespace CSDTP.Protocols.Abstracts
{
    internal abstract class BaseSender : ISender
    {
        public IPEndPoint Destination { get; }

        public bool IsAvailable { get; protected set; } = true;

        public BaseSender(IPEndPoint destination)
        {
            Destination = destination;
        }

        public abstract void Dispose();

        public abstract Task<bool> SendBytes(byte[] bytes);
    }
}
