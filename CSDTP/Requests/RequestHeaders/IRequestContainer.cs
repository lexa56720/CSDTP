using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests.RequestHeaders
{
    public enum RequestType
    {
        Post,
        Get,
        Response,
    }
    public interface IRequestContainer
    {
        public Guid Id { get; set; }

        public Type DataType { get; set; }

        public object DataObj { get; set; }

        public RequestType RequestType { get; set; }

    }
}
