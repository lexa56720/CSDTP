using CSDTP.Protocols.Abstracts;
using System.Net;
using System.Net.Sockets;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                    await Listener.ReceiveAsync(token).AsTask().ContinueWith(HandleData, token, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task HandleData(Task<UdpReceiveResult> udpResultTask, object? state)
        {
            if (state is not CancellationToken token)
                return;

            var data = await udpResultTask;
            token.ThrowIfCancellationRequested();

            OnDataAppear(data.Buffer, data.RemoteEndPoint.Address);
        }
    }
}
