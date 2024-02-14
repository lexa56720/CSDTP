using AutoSerializer;

namespace CSDTP.Requests.RequestHeaders
{
    public enum RequesKind
    {
        Request,
        Data,
        Response,
    }

    public interface IRequestContainer
    {
        public Guid Id { get; set; }

        public Type DataType { get; set; }

        public object DataObj { get; set; }

        public Type? ResponseObjType { get; set; }

        public RequesKind RequestKind { get; set; }

    }
}
