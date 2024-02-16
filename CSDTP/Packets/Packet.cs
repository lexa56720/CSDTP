using AutoSerializer;
using System.Net;

namespace CSDTP.Packets
{
    public class Packet<TData> : IPacket<TData> where TData : ISerializable<TData>, new()
    {
        public bool IsHasData;

        public TData? Data { get; set; }
        public object? DataObj => Data;

        public Type TypeOfPacket { get; private set; }

        public int ReplyPort { get; internal set; }

        public DateTime SendTime { get; internal set; }

        public DateTime ReceiveTime { get; set; }

        public IPAddress? Source { get; set; }

        public Packet(TData data)
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
                Data = TData.Deserialize(reader);
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
