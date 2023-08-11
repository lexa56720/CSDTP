using CSDTP.Protocols.Abstracts;
using CSDTP.Protocols.Udp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols
{
    public class Sender : ISender
    {
        public IPEndPoint Destination => SenderSocket.Destination;

        public int ReplyPort => SenderSocket.ReplyPort;


        private BaseSender SenderSocket;

        public Sender(IPEndPoint destination, bool isTcp = false, int replyPort = -1)
        {

            if (isTcp)
            {

            }
            else
            {
                SenderSocket = new UdpSender(destination, replyPort);
            }

        }

        public void Dispose()
        {
            SenderSocket.Dispose();
        }

        public Task<bool> Send<T>(T data) where T : ISerializable<T>
        {
            return SenderSocket.Send(data);
        }
    }
}
