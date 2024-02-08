using CSDTP.Utils;
using CSDTP.Utils.Performance;

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
            if (requestType == RequesKind.Request)
            {
                var resposeObjType = GlobalByteDictionary<Type>.Get(reader.ReadByteArray(),
                                                                    b => Type.GetType(Compressor.Decompress(b)));
                return new RequestContainer<T>(T.Deserialize(reader), id, requestType)
                {
                    ResponseObjType = resposeObjType
                };
            }
            return new RequestContainer<T>(T.Deserialize(reader), id, requestType);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id.ToByteArray());
            writer.Write((byte)RequestKind);
            if (RequestKind == RequesKind.Request)
                writer.WriteBytes(Compressor.Compress(ResponseObjType.AssemblyQualifiedName));
            Data.Serialize(writer);
        }
    }
}
