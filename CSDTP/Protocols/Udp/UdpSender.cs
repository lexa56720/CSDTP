using CSDTP.Protocols.Abstracts;
using System.Net;
using System.Net.Sockets;

namespace CSDTP.Protocols.Udp
{

    internal class UdpSender : BaseSender
    {
        private readonly UdpClient client;

        private CancellationTokenSource CancellationToken { get; set; } = new CancellationTokenSource();
        public UdpSender(IPEndPoint destination) : base(destination)
        {
            client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            client.Connect(Destination);
        }
        public override void Dispose()
        {
            if (IsDisposed)
                return;
            IsAvailable = false;
            IsDisposed = true;
            CancellationToken.Cancel();
            CancellationToken.Dispose();
            client.Dispose();
        }

        public override async Task<bool> SendBytes(byte[] bytes)
        {
            if (!IsAvailable)
                return false;

            try
            {
                if (!IsAvailable)
                    return false;

                var sended = await client.SendAsync(bytes, CancellationToken.Token);
                return sended == bytes.Length;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}
