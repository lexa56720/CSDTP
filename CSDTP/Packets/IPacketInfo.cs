using CSDTP.Cryptography.Algorithms;
using CSDTP.Cryptography.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Packets
{
    public interface IPacketInfo
    {
        public IPAddress? Source { get; set; }

        public object? InfoObj { get; set; }

        public int ReplyPort { get; }

        public DateTime SendTime { get; }

        public DateTime ReceiveTime { get; set; }
    }
}
