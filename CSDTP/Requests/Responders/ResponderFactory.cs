﻿using CSDTP.Cryptography.Providers;
using CSDTP.Protocols;

namespace CSDTP.Requests
{
    public static class ResponderFactory
    {

        public static Responder Create(Protocol protocol)
        {
            return new Responder(ReceiverFactory.CreateReceiver(protocol), null, null) { Protocol = protocol };
        }
        public static Responder Create(IEncryptProvider encryptProvider, Protocol protocol = Protocol.Udp)
        {
            return new Responder(ReceiverFactory.CreateReceiver(protocol), encryptProvider, null) { Protocol = protocol };
        }
        public static Responder Create(Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return new Responder(ReceiverFactory.CreateReceiver(protocol), null, customPacketType) { Protocol = protocol };
        }
        public static Responder Create(IEncryptProvider encryptProvider, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return new Responder(ReceiverFactory.CreateReceiver(protocol), encryptProvider, customPacketType) { Protocol = protocol };
        }



        public static Responder Create(int port, IEncryptProvider encryptProvider, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return new Responder(ReceiverFactory.CreateReceiver(port, protocol), encryptProvider, customPacketType) { Protocol = protocol };
        }

        public static Responder Create(int port, Protocol protocol = Protocol.Udp)
        {
            return new Responder(ReceiverFactory.CreateReceiver(port, protocol), null, null) { Protocol = protocol }; ;
        }

        public static Responder Create(int port, IEncryptProvider encryptProvider, Protocol protocol = Protocol.Udp)
        {
            return new Responder(ReceiverFactory.CreateReceiver(port, protocol), encryptProvider, null) { Protocol = protocol }; ;
        }

        public static Responder Create(int port, Type customPacketType, Protocol protocol = Protocol.Udp)
        {
            return new Responder(ReceiverFactory.CreateReceiver(port, protocol), null, customPacketType) { Protocol = protocol };
        }
    }
}
