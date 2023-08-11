using CSDTP.Packets;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP
{
    internal class Packet<T> : IPacket where T : ISerializable<T>
    {

        public bool IsHasData;

        public T? Data;

        public Type TypeOfPacket => throw new NotImplementedException();

        public Packet(T data)
        {
            Data = data;
            IsHasData = true;
        }

        public Packet()
        {

        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(typeof(Packet<T>).FullName);
            writer.Write(IsHasData);
            if (IsHasData)
                Data.Serialize(writer);
        }

        public IPacket Deserialize(BinaryReader reader)
        {
            IsHasData = reader.ReadBoolean();
            if (IsHasData)
               Data = T.Deserialize(reader);
            return this;
        }
    }
}
