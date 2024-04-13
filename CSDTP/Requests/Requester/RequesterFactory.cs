using CSDTP.Cryptography.Providers;
using CSDTP.Protocols;
using System.Net;

namespace CSDTP.Requests
{
    public static class RequesterFactory
    {
        public static async Task<Requester> Create(IPEndPoint destination, int replyPort, Protocol protocol = Protocol.Udp)
        {
            return await Requester.Initialize(CommunicatorFactory.Create(destination, replyPort, protocol));
        }
        public static async Task<Requester> Create(IPEndPoint destination, int replyPort, IEncryptProvider encryptProvider, Protocol protocol = Protocol.Udp)
        {
            return await Requester.Initialize(CommunicatorFactory.Create(destination, replyPort, protocol),
                                              encryptProvider);
        }
        public static async Task<Requester> Create(IPEndPoint destination, int replyPort, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return await Requester.Initialize(CommunicatorFactory.Create(destination, replyPort, protocol),
                                              null, customPacketType);
        }

        public static async Task<Requester> Create(IPEndPoint destination, int replyPort, IEncryptProvider encryptProvider, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return await Requester.Initialize(CommunicatorFactory.Create(destination, replyPort, protocol),
                                 encryptProvider,
                                 customPacketType);
        }



        public static async Task<Requester> Create(IPEndPoint destination, Protocol protocol = Protocol.Udp)
        {
            return await Requester.Initialize(CommunicatorFactory.Create(destination, protocol));
        }
        public static async Task<Requester> Create(IPEndPoint destination, IEncryptProvider encryptProvider, Protocol protocol = Protocol.Udp)
        {
            return await Requester.Initialize(CommunicatorFactory.Create(destination, protocol), encryptProvider);
        }
        public static async Task<Requester> Create(IPEndPoint destination, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return await Requester.Initialize(CommunicatorFactory.Create(destination, protocol),
                                              null, customPacketType);
        }

        public static async Task<Requester> Create(IPEndPoint destination, IEncryptProvider encryptProvider, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return await Requester.Initialize(CommunicatorFactory.Create(destination, protocol),
                                              encryptProvider, customPacketType);
        }
    }
}
