using CSDTP.Cryptography.Algorithms;
using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Utils;
using CSDTP.Utils.Performance;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Packets
{
    public class Packet<T> : IPacket where T : ISerializable<T>
    {
        public bool IsHasData;

        public T? Data;
        public object? DataObj => Data;

        public CryptMethod CryptMethod { get; private set; } = CryptMethod.None;

        public Type TypeOfPacket { get; private set; }

        public int ReplyPort { get; internal set; }

        public DateTime SendTime { get; internal set; }

        public DateTime ReceiveTime { get; set; }

        public IPAddress Source { get; set; }

        public object? InfoObj { get; set; }

        public Packet(T data)
        {
            Data = data;
            IsHasData = true;
            TypeOfPacket = GetType();
        }

        public Packet()
        {
            TypeOfPacket = GetType();
        }

        public void Serialize(BinaryWriter writer, IEncryptProvider encryptProvider)
        {
            SerializePacketHeaders(writer);
            CryptData(writer, encryptProvider);
        }
        public void Serialize(BinaryWriter writer)
        {
            SerializePacketHeaders(writer);
            writer.Write((byte)CryptMethod.None);
            if (IsHasData)
                Data.Serialize(writer);
        }
        private void SerializePacketHeaders(BinaryWriter writer)
        {
            var typeBytes = Compressor.Compress(TypeOfPacket.AssemblyQualifiedName);
            writer.WriteBytes(typeBytes);

            SerializeCustomData(writer);
            writer.Write(ReplyPort);
            writer.Write(SendTime.ToBinary());

            writer.Write(IsHasData);
        }
        protected virtual void SerializeCustomData(BinaryWriter writer)
        {

        }
        private void CryptData(BinaryWriter writer, IEncryptProvider encryptProvider)
        {
            var crypter = encryptProvider.GetEncrypter(this);
            writer.Write((byte)crypter.CryptMethod);
            if (IsHasData)
            {
                using var ms = new MemoryStream();
                using var cryptWriter = new BinaryWriter(ms);
                Data.Serialize(cryptWriter);
                writer.Write(crypter.Crypt(ms.ToArray()));
            }
        }


        public IPacket Deserialize(BinaryReader reader, IEncryptProvider encryptProvider)
        {
            DeserializePacketHeaders(reader);


            if (IsHasData && CryptMethod != CryptMethod.None)
                Data = DecryptData<T>(reader, encryptProvider);
            else
                Data = T.Deserialize(reader);

            return this;
        }
        public IPacket Deserialize(BinaryReader reader)
        {
            DeserializePacketHeaders(reader);

            Data = T.Deserialize(reader);

            return this;
        }
        private void DeserializePacketHeaders(BinaryReader reader)
        {
            DeserializeCustomData(reader);
            ReplyPort = reader.ReadInt32();
            SendTime = DateTime.FromBinary(reader.ReadInt64());
            CryptMethod = (CryptMethod)reader.ReadByte();
            IsHasData = reader.ReadBoolean();
        }
        protected virtual void DeserializeCustomData(BinaryReader reader)
        {

        }
        private T DecryptData<T>(BinaryReader reader, IEncryptProvider encryptProvider) where T : ISerializable<T>
        {
            var crypter = encryptProvider.GetDecrypter(this);
            ArgumentNullException.ThrowIfNull(crypter);

            var ms = (MemoryStream)reader.BaseStream;

            var bytes = ms.ToArray();

            var decrypted = crypter.Decrypt(bytes, (int)ms.Position, (int)(ms.Length - ms.Position));
            encryptProvider.DisposeEncryptor(crypter);

            using var decryptedMS = new MemoryStream(decrypted);
            using var br = new BinaryReader(decryptedMS);
            return T.Deserialize(br);
        }

    }
}
