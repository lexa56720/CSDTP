using System.Net;

namespace CSDTP.Protocols.Abstracts
{
    public interface ISender : IDisposable
    {
        public IPEndPoint Destination { get; }
        public bool IsAvailable { get; }
        public Task<bool> SendBytes(byte[] bytes);
    }
}
