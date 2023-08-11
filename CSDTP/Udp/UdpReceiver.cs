using CSDTP.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Udp
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

                    Task.Run(() =>
                    {
                        var packet = GetPacket(data.Buffer);
                        OnDataAppear(packet);
                    });
                }
            });
        }

        public override void Stop()
        {
            base.Stop();
        }

        protected IPacket GetPacket(byte[] bytes)
        {
            using var reader = new BinaryReader(new MemoryStream(bytes));
            var packet = (IPacket)Activator.CreateInstance(Type.GetType(reader.ReadString()));
            packet.Deserialize(reader);
            return packet;
        }
    }
}
