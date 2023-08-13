using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests.RequestHeaders
{
    enum RequestType
    {
        Post,
        Get,
        Response,
    }
    internal interface IRequestContainer
    {
        public Guid Id { get; set; }

        public Type DataType { get; }

        public object DataObj { get; }

        public RequestType RequestType { get; set; }

    }
}
