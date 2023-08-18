using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Abstracts
{
    public abstract class BaseSender : ISender
    {
        public IPEndPoint Destination { get; }
        public int ReplyPort { get; }

        public bool IsAvailable { get; protected set; } = true;
        public IEncryptProvider? EncryptProvider { get; set; }

        public BaseSender(IPEndPoint destination)
        {
            Destination = destination;
            ReplyPort = 0;
        }

        public BaseSender(IPEndPoint destination, int replyPort = -1)
        {
            Destination = destination;
            ReplyPort = replyPort;
        }

        public BaseSender(IPEndPoint destination, IEncryptProvider encryptProvider)
        {
            Destination = destination;
            EncryptProvider = encryptProvider;
            ReplyPort = 0;
        }

        public BaseSender(IPEndPoint destination, IEncryptProvider encryptProvider, int replyPort = -1)
        {
            Destination = destination;
            EncryptProvider = encryptProvider;
            ReplyPort = replyPort;
        }

        public abstract void Dispose();
        public async Task<bool> Send<T>(T data) where T : ISerializable<T> 
        {
            if (EncryptProvider == null)
                return await SendBytes(GetBytes<T, Packet<T>>(data));
            else
                return await SendBytes(GetBytes<T, Packet<T>>(data, EncryptProvider));
        }
        public async Task<bool> Send<T,U>(T data) where T : ISerializable<T> where U : Packet<T>, new()
        {
            if (EncryptProvider == null)
                return await SendBytes(GetBytes<T,U>(data));
            else
                return await SendBytes(GetBytes<T, U>(data, EncryptProvider));
        }

        protected abstract Task<bool> SendBytes(byte[] bytes);

        private byte[] GetBytes<T,U>(T data) where T : ISerializable<T> where U : Packet<T>, new()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            GetPacket<T, U>(data).Serialize(writer);
            return ms.ToArray();
        }

        private byte[] GetBytes<T,U>(T data, IEncryptProvider encryptProvider) where T : ISerializable<T> where U : Packet<T>, new()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            GetPacket<T,U>(data).Serialize(writer, encryptProvider);

            return ms.ToArray();
        }

        private Packet<T> GetPacket<T,U>(T data) where T : ISerializable<T> where U:Packet<T>,new()
        {
            return new U()
            {
                Data = data,
                IsHasData=true,
                ReplyPort = ReplyPort,
                SendTime = DateTime.Now,
            };
        }

        public abstract void Close();
    }
}
