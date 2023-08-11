using CSDTP.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Udp
{
    internal class UdpReceiver : BaseReceiver
    {
        private UdpClient Listener;
        public UdpReceiver(int port) : base(port)
        {
            Listener = new UdpClient(port);
        }

        public override void Dispose()
        {
            Listener.Close();
        }

        public override void Start()
        {
            base.Start();

            Task.Run(async () =>
            {
                while (IsReceiving)
                {
                    var data = await Listener.ReceiveAsync();

                    if (!IsReceiving)
                        return;

                    ReceiverQueue.Enqueue(data.Buffer);
                }
            });
        }

        public override void Stop()
        {
            base.Stop();
        }


    }
}
