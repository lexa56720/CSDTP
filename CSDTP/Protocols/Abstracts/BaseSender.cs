using CSDTP.Cryptography;
using CSDTP.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CSDTP.Protocols.Abstracts
{
    public abstract class BaseSender : ISender
    {
        public IPEndPoint Destination { get; }
        public int ReplyPort { get; }

        public bool IsAvailable { get; protected set; } = true;

        public BaseSender(IPEndPoint destination, int replyPort = -1)
        {
            Destination = destination;
            ReplyPort = replyPort;
        }

        public abstract void Dispose();

        public async Task<bool> Send<T>(T data) where T : ISerializable<T>
        {
            return await SendBytes(GetBytes(data));
        }
        public async Task<bool> Send<T>(T data, IEncrypter encrypter) where T : ISerializable<T>
        {
            return await SendBytes(GetBytes(data, encrypter));
        }

        protected abstract Task<bool> SendBytes(byte[] bytes);

        private byte[] GetBytes<T>(T data) where T : ISerializable<T>
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            GetPacket(data).Serialize(writer);
            return AppendToStart(ms.ToArray(), 0); 
        }

        private byte[] GetBytes<T>(T data, IEncrypter encrypter) where T : ISerializable<T>
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            GetPacket(data).Serialize(writer);
     
            var bytes = ms.ToArray();
            return AppendToStart(encrypter.Crypt(bytes),1);
        }

        private byte[] AppendToStart(byte[] array,byte value)
        {
            byte[] newArray = new byte[array.Length + 1];
            newArray[0] = value;                               
            Array.Copy(array, 0, newArray, 1, array.Length);
            return newArray;
        }

        private Packet<T> GetPacket<T>(T data) where T : ISerializable<T>
        {
            return new Packet<T>(data)
            {
                ReplyPort = ReplyPort,
                SendTime = DateTime.Now,
            };
        }

        public abstract void Close();
    }
}
