using AutoSerializer;
using PerformanceUtils.Performance;

namespace CSDTP.Requests.RequestHeaders
{
    internal class RequestContainer<T> : IRequestContainer, ISerializable<RequestContainer<T>> where T : ISerializable<T>, new()
    {
        public Guid Id { get; set; }

        public T Data { get; private set; }

        public RequesKind RequestKind { get; set; }

        public Type DataType { get; set; }

        public Type? ResponseObjType { get; set; }

        public object DataObj
        {
            get
            {
                return Data;
            }
            set
            {
                Data = (T)value;
            }
        }

        public RequestContainer(T data, RequesKind type)
        {
            Data = data;
            Id = Guid.NewGuid();
            RequestKind = type;
            DataType = typeof(T);
        }
        public RequestContainer(T data, Guid id, RequesKind type)
        {
            Data = data;
            Id = id;
            RequestKind = type;
            DataType = typeof(T);
        }
        public RequestContainer() { }

        public static RequestContainer<T> Deserialize(BinaryReader reader)
        {
            var id = new Guid(reader.ReadBytes(16));
            var requestType = (RequesKind)reader.ReadByte();
            var result = new RequestContainer<T>(T.Deserialize(reader), id, requestType);
            if (requestType == RequesKind.Request)
            {
                result.ResponseObjType = typeof(T);
            }
            return result;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id.ToByteArray());
            writer.Write((byte)RequestKind);
            Data.Serialize(writer);
        }
    }
}
