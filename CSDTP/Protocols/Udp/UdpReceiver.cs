using CSDTP.Protocols.Abstracts;
using System.Net;
using System.Net.Sockets;

namespace CSDTP.Protocols.Udp
{
    internal class UdpReceiver : BaseReceiver
    {
        private UdpClient Listener;

        public override int Port => ((IPEndPoint)Listener.Client.LocalEndPoint).Port;
        public UdpReceiver(int port) : base(port)
        {
            Listener = new UdpClient(port);
        }
        public UdpReceiver() : base()
        {
            Listener = new UdpClient(0);
        }
        public override void Dispose()
        {
            base.Dispose();
            Listener.Dispose();
        }


        protected override async Task ReceiveWork(CancellationToken token)
        {
            while (IsReceiving)
            {
                try
                {
                    var data = await Listener.ReceiveAsync(token);

                    token.ThrowIfCancellationRequested();

                    OnDataAppear(data.Buffer, data.RemoteEndPoint.Address);

                }
                catch (OperationCanceledException)
                {
                    return;
                }

            }
        }
    }
}
