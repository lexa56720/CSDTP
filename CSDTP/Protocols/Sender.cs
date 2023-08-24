using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
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

        public bool IsAvailable => SenderSocket.IsAvailable;

        public IEncryptProvider? EncryptProvider
        {
            get => SenderSocket.EncryptProvider;
            set => SenderSocket.EncryptProvider = value;
        }

        private BaseSender SenderSocket;

        public Sender(IPEndPoint destination, bool isTcp = false)
        {
            if (isTcp)
                throw new NotImplementedException();
            else
                SenderSocket = new UdpSender(destination);
        }
        public Sender(IPEndPoint destination, int replyPort, bool isTcp = false)
        {
            if (isTcp)
                throw new NotImplementedException();
            else
                SenderSocket = new UdpSender(destination, replyPort);
        }
        public Sender(IPEndPoint destination, IEncryptProvider encryptProvider, bool isTcp = false)
        {
            if (isTcp)
                throw new NotImplementedException();
            else
                SenderSocket = new UdpSender(destination, encryptProvider);
        }
        public Sender(IPEndPoint destination, IEncryptProvider encryptProvider, int replyPort, bool isTcp = false)
        {

            if (isTcp)
                throw new NotImplementedException();
            else
                SenderSocket = new UdpSender(destination,encryptProvider, replyPort);
        }


        public void Dispose()
        {
            SenderSocket.Dispose();
        }


        public void Close()
        {
            SenderSocket.Close();
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
