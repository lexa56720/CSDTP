using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Udp
{
    internal class UdpSender<T> : BaseSender<T> where T : ISerializable<T>
    {
        private UdpClient Client { get; set; }
        public UdpSender(IPEndPoint destination, int replyPort) : base(destination, replyPort)
        {
            Client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            Client.Connect(Destination);
        }

        public override void Dispose()
        {
            Client.Close();
        }

        public override async Task<bool> Send(T data)
        {
            using var ms = new MemoryStream();
            data.Serialize(new BinaryWriter(ms));
            var bytes = ms.ToArray();
            var sended = await Client.SendAsync(bytes, bytes.Length);
            return sended==bytes.Length;
        }
    }
}
