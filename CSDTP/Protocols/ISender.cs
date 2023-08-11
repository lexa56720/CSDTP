using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols
{
    internal interface ISender: IDisposable
    {
        public IPEndPoint Destination { get; }
        public int ReplyPort { get; }

        public abstract Task<bool> Send<T>(T data) where T : ISerializable<T>;
    }
}
