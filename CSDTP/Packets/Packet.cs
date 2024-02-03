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
    public class Packet<T> : IPacket where T : ISerializable<T>, new()
    {
        public bool IsHasData;

        public T? Data;
        public object? DataObj => Data;

        public Type TypeOfPacket { get; private set; }

        public int ReplyPort { get; internal set; }

        public DateTime SendTime { get; internal set; }

        public DateTime ReceiveTime { get; set; }

        public IPAddress? Source { get; set; }

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


        public void SerializePacket(BinaryWriter writer)
        {
            writer.Write(ReplyPort);
            writer.Write(SendTime.ToBinary());
            writer.Write(IsHasData);

            Data?.Serialize(writer);
        }
        public virtual void SerializeUnprotectedCustomData(BinaryWriter writer)
        {
            return;
        }
        public virtual void SerializeProtectedCustomData(BinaryWriter writer)
        {
            return;
        }


        public void DeserializePacket(BinaryReader reader)
        {
            ReplyPort = reader.ReadInt32();
            SendTime = DateTime.FromBinary(reader.ReadInt64());
            IsHasData = reader.ReadBoolean();

            if (IsHasData)
                T.Deserialize(reader);
        }
        public virtual void DeserializeUnprotectedCustomData(BinaryReader writer)
        {
            return;
        }
        public virtual void DeserializeProtectedCustomData(BinaryReader writer)
        {
            return;
        }
    }
}
