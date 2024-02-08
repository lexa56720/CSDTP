using CSDTP.Cryptography.Providers;
using CSDTP.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    public static class ResponderFactory
    {

        public static Responder Create(Protocol protocol, IEncryptProvider encryptProvider, Type customPacketType)
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

        public static Responder Create(Protocol protocol, IEncryptProvider encryptProvider)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(encryptProvider, null),
                Protocol.Http => new HttpResponder(encryptProvider, null),
                _ => throw new NotImplementedException(),
            };
        }

        public static Responder Create(Protocol protocol, Type customPacketType)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(null, customPacketType),
                Protocol.Http => new HttpResponder(null, customPacketType),
                _ => throw new NotImplementedException(),
            };
        }


        public static Responder Create(Protocol protocol, int port, IEncryptProvider encryptProvider, Type customPacketType)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(port, encryptProvider, customPacketType),
                Protocol.Http => new HttpResponder(port, encryptProvider, customPacketType),
                _ => throw new NotImplementedException(),
            };
        }

        public static Responder Create(Protocol protocol, int port)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(port, null, null),
                Protocol.Http => new HttpResponder(port, null, null),
                _ => throw new NotImplementedException(),
            };
        }

        public static Responder Create(Protocol protocol, int port, IEncryptProvider encryptProvider)
        {
            return protocol switch
            {
                Protocol.Udp => new UdpResponder(port, encryptProvider, null),
                Protocol.Http => new HttpResponder(port, encryptProvider, null),
                _ => throw new NotImplementedException(),
            };
        }

        public static Responder Create(Protocol protocol, int port, Type customPacketType)
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
