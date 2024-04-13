using CSDTP.Protocols.Abstracts;
using CSDTP.Utils;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

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
        public override async ValueTask Start()
        {
            if (IsReceiving)
                return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                await PortUtils.PortForward(Port, "csdtp", false);

            await base.Start();
        }
        public override async ValueTask Stop()
        {
            if (!IsReceiving)
                return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                await PortUtils.PortBackward(Port, "csdtp", false);

            await base.Stop();
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
                finally
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        await PortUtils.PortBackward(Port, "csdtp", false);
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
