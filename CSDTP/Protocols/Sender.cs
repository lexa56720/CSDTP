using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols.Abstracts;
using CSDTP.Protocols.Http;
using CSDTP.Protocols.Udp;
using Open.Nat;
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

        public bool IsAvailable => SenderSocket.IsAvailable;

        public IEncryptProvider? EncryptProvider
        {
            get => SenderSocket.EncryptProvider;
            set => SenderSocket.EncryptProvider = value;
        }

        private BaseSender SenderSocket;

        public Sender(IPEndPoint destination, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    SenderSocket = new UdpSender(destination); 
                    break;
                case Protocol.Tcp:
                    throw new NotImplementedException("TCP NOT IMPLEMENTED");
                    break;
                case Protocol.Http:
                    SenderSocket = new HttpSender(destination);
                    break;
            }
        }
        public Sender(IPEndPoint destination, int replyPort, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    SenderSocket = new UdpSender(destination, replyPort);
                    break;
                case Protocol.Tcp:
                    throw new NotImplementedException("TCP NOT IMPLEMENTED");
                    break;
                case Protocol.Http:
                    SenderSocket = new HttpSender(destination, replyPort);
                    break;
            }
        }
        public Sender(IPEndPoint destination, IEncryptProvider encryptProvider, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    SenderSocket = new UdpSender(destination, encryptProvider); 
                    break;
                case Protocol.Tcp:
                    throw new NotImplementedException("TCP NOT IMPLEMENTED");
                    break;
                case Protocol.Http:
                    SenderSocket = new HttpSender(destination, encryptProvider);
                    break;
            }
        }
        public Sender(IPEndPoint destination, IEncryptProvider encryptProvider, int replyPort, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    SenderSocket = new UdpSender(destination, encryptProvider, replyPort); 
                    break;
                case Protocol.Tcp:
                    throw new NotImplementedException("TCP NOT IMPLEMENTED");
                    break;
                case Protocol.Http:
                    SenderSocket = new HttpSender(destination, encryptProvider, replyPort);
                    break;
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

        public Task<bool> Send<T, U>(T data)
            where T : ISerializable<T>
            where U : Packet<T>, new()
        {
            return SenderSocket.Send<T, U>(data);
        }

        public Task<bool> Send<T>(T data, object info) where T : ISerializable<T>
        {
            return SenderSocket.Send(data, info);
        }

        public Task<bool> Send<T, U>(T data, object info)
            where T : ISerializable<T>
            where U : Packet<T>, new()
        {
            return SenderSocket.Send<T, U>(data, info);
        }
    }
}
