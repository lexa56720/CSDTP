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

        public T Data { get; private set; }

        public RequestType RequestType { get; set; }

        public Type DataType { get; private set; }

        public object DataObj => Data;

        public RequestContainer(T data,RequestType type)
        {
            Data = data;
            Id = new Guid();
            RequestType = type;
            DataType = typeof(T);
        }

        public RequestContainer(T data, Guid id)
        {
            Data = data;
            Id = id;
            DataType = typeof(T);
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
