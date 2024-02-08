using CSDTP.Cryptography.Providers;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    internal class UdpResponder : Responder
    {
        public override Protocol Protocol => Protocol.Udp;

        internal UdpResponder(IEncryptProvider? encryptProvider = null, Type? customPacketType = null) :
            base(ReceiverFactory.CreateReceiver(Protocol.Udp), encryptProvider, customPacketType)
        {
        }

        internal UdpResponder(int port, IEncryptProvider? encryptProvider = null, Type? customPacketType = null) :
            base(ReceiverFactory.CreateReceiver(port, Protocol.Udp), encryptProvider, customPacketType)
        {
        }



        protected override ISender GetSender(IPEndPoint endPoint)
        {
            return SenderFactory.CreateSender(endPoint, Protocol.Udp);
        }


        protected override void Start(bool isRunning)
        {
        }

        protected override void Stop(bool isRunning)
        {
        }
    }
}
