using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CSDTP.Protocols.Abstracts;

namespace CSDTP.Protocols.Udp
{
    public class UdpSender : BaseSender
    {
        private UdpClient Client { get; set; }

        public UdpSender(IPEndPoint destination, int replyPort) : base(destination, replyPort)
        {
            Client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            Client.Connect(Destination);
        }

        public override void Dispose()
        {
                IsAvailable = false;
                Client.Dispose();
        }

        public override void Close()
        {
            Dispose();
        }

        public override async Task<bool> Send<T>(T data)
        {
            if (!IsAvailable)
                return false;
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            GetPacket(data).Serialize(writer);
            var bytes = ms.ToArray();

            var sended = await Client.SendAsync(bytes, bytes.Length);
            return sended == bytes.Length;
        }
    }
}
