using AutoSerializer;
using PerformanceUtils.Performance;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CSDTP.Requests.RequestHeaders
{
    internal class RequestContainer<TRequest> : IRequestContainer, ISerializable<RequestContainer<TRequest>>
                   where TRequest : ISerializable<TRequest>, new()
    {
        public Guid Id { get; set; }

        public TRequest Data { get; protected set; }

        public RequesKind RequestKind { get; set; }

        public virtual Type DataType { get; set; }
        public virtual Type? ResponseObjType { get; internal set; } = null;

        public object DataObj
        {
            get
            {
                return Data;
            }
            set
            {
                Data = (TRequest)value;
            }
        }


        public RequestContainer(TRequest data, RequesKind type)
        {
            Data = data;
            Id = Guid.NewGuid();
            RequestKind = type;
            DataType = typeof(TRequest);
        }
        public RequestContainer(TRequest data, Guid id, RequesKind type)
        {
            Data = data;
            Id = id;
            RequestKind = type;
            DataType = typeof(TRequest);
        }
        public RequestContainer() { }

        public static RequestContainer<TRequest> Deserialize(BinaryReader reader)
        {
            var id = new Guid(reader.ReadBytes(16));
            var requestType = (RequesKind)reader.ReadByte();
            var result = new RequestContainer<TRequest>(TRequest.Deserialize(reader), id, requestType);
            return result;
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write(Id.ToByteArray());
            writer.Write((byte)RequestKind);
            Data.Serialize(writer);
        }
    }

    internal class RequestContainer<TRequest, TResponse> : RequestContainer<TRequest>, ISerializable<RequestContainer<TRequest, TResponse>>
                   where TRequest : ISerializable<TRequest>, new() 
                   where TResponse : ISerializable<TResponse>, new()
    {
        public override Type? ResponseObjType { get; internal set; } = typeof(TResponse);
        public RequestContainer(TRequest data, RequesKind type)
        {
            Data = data;
            Id = Guid.NewGuid();
            RequestKind = type;
            DataType = typeof(TRequest);
        }
        public RequestContainer(TRequest data, Guid id, RequesKind type)
        {
            Data = data;
            Id = id;
            RequestKind = type;
            DataType = typeof(TRequest);
        }
        public RequestContainer() { }
        public new static RequestContainer<TRequest,TResponse> Deserialize(BinaryReader reader)
        {
            var id = new Guid(reader.ReadBytes(16));
            var requestType = (RequesKind)reader.ReadByte();
            var result = new RequestContainer<TRequest, TResponse>(TRequest.Deserialize(reader), id, requestType);
            if (requestType == RequesKind.Request)
            {
                result.ResponseObjType = typeof(TResponse);
            }
            return result;
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Id.ToByteArray());
            writer.Write((byte)RequestKind);
            Data.Serialize(writer);
        }
    }
}
