using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Abstracts
{
    public interface ISender : IDisposable
    {
        public IPEndPoint Destination { get; }

        public int ReplyPort { get; }
        public bool IsAvailable { get; }


        public Task<bool> Send(byte[] bytes);
    }
}
