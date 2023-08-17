using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CSDTP.Cryptography.Providers;
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

        public UdpSender(IPEndPoint destination) : base(destination)
        {
            Client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            Client.Connect(Destination);
        }
        public UdpSender(IPEndPoint destination,IEncryptProvider encryptProvider, int replyPort) : base(destination, encryptProvider, replyPort)
        {
            Client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            Client.Connect(Destination);
        }

        public UdpSender(IPEndPoint destination, IEncryptProvider encryptProvider) : base(destination, encryptProvider)
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

        protected override async Task<bool> SendBytes(byte[] bytes)
        {
            if (!IsAvailable)
                return false;

            var sended = await Client.SendAsync(bytes, bytes.Length);
            return sended == bytes.Length;
        }
    }
}
