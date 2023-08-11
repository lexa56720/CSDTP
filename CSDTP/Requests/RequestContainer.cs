using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    internal class RequestContainer<T> : IRequestContainer, ISerializable<RequestContainer<T>> where T : ISerializable<T>
    {
        public Guid Id { get; set; }

        public T Data { get; set; }
        public RequestContainer(T data)
        {
            Data = data;
            Id = new Guid();
        }

        public RequestContainer(T data, Guid id)
        {
            Data = data;
            Id = id;
        }
        public RequestContainer()
        {

        }
        public static RequestContainer<T> Deserialize(BinaryReader reader)
        {
            var id = new Guid(reader.ReadBytes(16));
            return new RequestContainer<T>(T.Deserialize(reader),id);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id.ToByteArray());
            Data.Serialize(writer);
        }
    }
}
