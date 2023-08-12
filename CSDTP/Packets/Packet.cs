using CSDTP.Packets;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP
{
    public class Packet<T> : IPacket where T : ISerializable<T>
    {
        public bool IsHasData;

        public T? Data;
        public object DataObj => Data;

        public Type TypeOfPacket { get; private set; }

        public int ReplyPort { get; internal set; }

        public DateTime SendTime { get; internal set; }

        public DateTime ReceiveTime {  get;  set; }

        public IPAddress Source { get; set; }

        public Packet(T data)
        {
            Data = data;
            IsHasData = true;
            TypeOfPacket= typeof(Packet<T>);
        }

        public Packet()
        {
            TypeOfPacket = typeof(Packet<T>);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(typeof(Packet<T>).FullName);

            writer.Write(ReplyPort);
            writer.Write(SendTime.ToBinary());

            writer.Write(IsHasData);
            if (IsHasData)
                Data.Serialize(writer);
        }

        public IPacket Deserialize(BinaryReader reader)
        {
            ReplyPort= reader.ReadInt32();    
            SendTime = DateTime.FromBinary(reader.ReadInt64());

            IsHasData = reader.ReadBoolean();
            if (IsHasData)
               Data = T.Deserialize(reader);

            return this;
        }
    }
}
