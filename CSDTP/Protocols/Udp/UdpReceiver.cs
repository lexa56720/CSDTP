using CSDTP.Packets;
using CSDTP.Protocols.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Udp
{
    internal class UdpReceiver : BaseReceiver
    {
        private UdpClient Listener;

        private CancellationTokenSource TokenSource = new CancellationTokenSource();
        public UdpReceiver(int port) : base(port)
        {
            Listener = new UdpClient(port);
        }

        public override void Close()
        {
            Dispose();
        }

        public override void Dispose()
        {
            Stop();
            TokenSource.Cancel();
            TokenSource.Dispose();
            Listener.Dispose();
        }

        public override void Start()
        {
            base.Start();
            TokenSource.Dispose();
            TokenSource = new CancellationTokenSource();
            var token = TokenSource.Token;

            Task.Run(async () =>
            {
                while (IsReceiving)
                {
                    try
                    {
                        var data = await Listener.ReceiveAsync(token);

                        token.ThrowIfCancellationRequested();

                        ReceiverQueue.Add(new Tuple<byte[], IPAddress>(data.Buffer, data.RemoteEndPoint.Address));
                    }
                    catch (OperationCanceledException e)
                    {
                        return;
                    }

                }
            });
        }
    }
}
