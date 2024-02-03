using CSDTP.Utils;
using CSDTP.Utils.Performance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests.RequestHeaders
{
    internal class RequestContainer<T> : IRequestContainer, ISerializable<RequestContainer<T>> where T : ISerializable<T>,new()
    {
        public Guid Id { get; set; }

        public T Data { get; private set; }

        public RequestType RequestType { get; set; }

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

        public RequestContainer(T data, RequestType type)
        {
            Data = data;
            Id = Guid.NewGuid();
            RequestType = type;
            DataType = typeof(T);
        }
        public RequestContainer(T data, Guid id, RequestType type)
        {
            Data = data;
            Id = id;
            RequestType = type;
            DataType = typeof(T);
        }
        public RequestContainer() { }

        public static RequestContainer<T> Deserialize(BinaryReader reader)
        {
            var id = new Guid(reader.ReadBytes(16));
            var requestType = (RequestType)reader.ReadByte();
            if (requestType == RequestType.Post)
            {
                var resposeObjType = GlobalByteDictionary<Type>.Get(reader.ReadByteArray(), 
                                                                    b => Type.GetType(Compressor.Decompress(b)));
                return new RequestContainer<T>(T.Deserialize(reader), id, requestType)
                {
                    ResponseObjType=resposeObjType
                };
            }
            return new RequestContainer<T>(T.Deserialize(reader), id, requestType);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id.ToByteArray());
            writer.Write((byte)RequestType);
            if (RequestType == RequestType.Post)
                writer.WriteBytes(Compressor.Compress(ResponseObjType.AssemblyQualifiedName));
            Data.Serialize(writer);
        }
    }
}
