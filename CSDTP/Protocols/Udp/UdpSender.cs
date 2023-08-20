using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CSDTP.Cryptography.Providers;
using CSDTP.Protocols.Abstracts;
using CSDTP.Utils;

namespace CSDTP.Protocols.Udp
{
    public class UdpSender : BaseSender
    {
        public UdpSender(IPEndPoint destination, int replyPort) : base(destination, replyPort)
        {
        }

        public UdpSender(IPEndPoint destination) : base(destination)
        {
        }
        public UdpSender(IPEndPoint destination,IEncryptProvider encryptProvider, int replyPort) : base(destination, encryptProvider, replyPort)
        {
        }

        public UdpSender(IPEndPoint destination, IEncryptProvider encryptProvider) : base(destination, encryptProvider)
        {
        }
        public override void Dispose()
        {
            IsAvailable = false;
        }

        public override void Close()
        {
            Dispose();
        }


        protected override async Task<bool> SendBytes(byte[] bytes)
        {
            var client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            client.Connect(Destination);
            if (!IsAvailable)
                return false;

            var sended = await client.SendAsync(bytes, bytes.Length);
            client.Close();
            return sended == bytes.Length;
        }
    }
}
