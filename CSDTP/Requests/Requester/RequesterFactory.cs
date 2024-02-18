using CSDTP.Cryptography.Providers;
using CSDTP.Protocols;
using System.Net;

namespace CSDTP.Requests
{
    public static class RequesterFactory
    {
        public static Requester Create(IPEndPoint destination, int replyPort, Protocol protocol = Protocol.Udp)
        {
            return new Requester(SenderFactory.CreateSender(destination, protocol), ReceiverFactory.CreateReceiver(replyPort, protocol));
        }
        public static Requester Create(IPEndPoint destination, int replyPort,  IEncryptProvider encryptProvider, Protocol protocol = Protocol.Udp)
        {
            return new Requester(SenderFactory.CreateSender(destination, protocol),
                                 ReceiverFactory.CreateReceiver(replyPort, protocol),
                                 encryptProvider);
        }
        public static Requester Create(IPEndPoint destination, int replyPort,  Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return new Requester(SenderFactory.CreateSender(destination, protocol),
                                 ReceiverFactory.CreateReceiver(replyPort, protocol),
                                 null, customPacketType);
        }

        public static Requester Create(IPEndPoint destination, int replyPort,IEncryptProvider encryptProvider, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return new Requester(SenderFactory.CreateSender(destination, protocol),
                                 ReceiverFactory.CreateReceiver(replyPort, protocol),
                                 encryptProvider,
                                 customPacketType);
        }



        public static Requester Create(IPEndPoint destination, Protocol protocol = Protocol.Udp)
        {
            return new Requester(SenderFactory.CreateSender(destination, protocol), ReceiverFactory.CreateReceiver(protocol));
        }
        public static Requester Create(IPEndPoint destination, IEncryptProvider encryptProvider, Protocol protocol = Protocol.Udp)
        {
            return new Requester(SenderFactory.CreateSender(destination, protocol),
                                 ReceiverFactory.CreateReceiver(protocol),
                                 encryptProvider);
        }
        public static Requester Create(IPEndPoint destination, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return new Requester(SenderFactory.CreateSender(destination, protocol),
                                 ReceiverFactory.CreateReceiver(protocol),
                                 null, customPacketType);
        }

        public static Requester Create(IPEndPoint destination, IEncryptProvider encryptProvider, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return new Requester(SenderFactory.CreateSender(destination, protocol),
                                 ReceiverFactory.CreateReceiver(protocol),
                                 encryptProvider,
                                 customPacketType);
        }
    }
}
