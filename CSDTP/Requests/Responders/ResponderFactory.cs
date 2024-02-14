using CSDTP.Cryptography.Providers;
using CSDTP.Protocols;

namespace CSDTP.Requests
{
    public static class ResponderFactory
    {

        public static Responder Create(IEncryptProvider encryptProvider, Type customPacketType, Protocol protocol=Protocol.Udp)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(encryptProvider, customPacketType),
                Protocol.Http => new HttpResponder(encryptProvider, customPacketType),
                _ => throw new NotImplementedException(),
            };
        }

        public static Responder Create(Protocol protocol)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(null, null),
                Protocol.Http => new HttpResponder(null, null),
                _ => throw new NotImplementedException(),
            };
        }

        public static Responder Create(IEncryptProvider encryptProvider, Protocol protocol = Protocol.Udp)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(encryptProvider, null),
                Protocol.Http => new HttpResponder(encryptProvider, null),
                _ => throw new NotImplementedException(),
            };
        }

        public static Responder Create( Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(null, customPacketType),
                Protocol.Http => new HttpResponder(null, customPacketType),
                _ => throw new NotImplementedException(),
            };
        }


        public static Responder Create(int port, IEncryptProvider encryptProvider, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(port, encryptProvider, customPacketType),
                Protocol.Http => new HttpResponder(port, encryptProvider, customPacketType),
                _ => throw new NotImplementedException(),
            };
        }

        public static Responder Create(int port,Protocol protocol = Protocol.Udp)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(port, null, null),
                Protocol.Http => new HttpResponder(port, null, null),
                _ => throw new NotImplementedException(),
            };
        }

        public static Responder Create( int port, IEncryptProvider encryptProvider, Protocol protocol = Protocol.Udp)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(port, encryptProvider, null),
                Protocol.Http => new HttpResponder(port, encryptProvider, null),
                _ => throw new NotImplementedException(),
            };
        }

        public static Responder Create(int port, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(port, null, customPacketType),
                Protocol.Http => new HttpResponder(port, null, customPacketType),
                _ => throw new NotImplementedException(),
            };
        }

    }
}
