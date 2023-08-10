using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP
{
    public class Packet<T> : ISerializable<Packet<T>> where T : ISerializable<T>
    {

        public bool IsHasData;

        public T? Data;

        public Packet(T data)
        {
            Data = data;
            IsHasData = true;
        }

        public Packet()
        {

        }

        public static Packet<T> Deserialize(BinaryReader reader)
        {
            var type = Type.GetType(reader.ReadString());


            var packet = new Packet<T>();
            packet.IsHasData = reader.ReadBoolean();
            if(packet.IsHasData)
                packet.Data=T.Deserialize(reader);
            return packet;
        }

        public void Serialize(BinaryWriter writer)
        {
            Console.WriteLine(typeof(Packet<T>).FullName);
            writer.Write(IsHasData);
            if (IsHasData)
                Data.Serialize(writer);
        }
    }
}
