using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols
{
    internal struct DataInfo
    {
        public required Func<byte[], Task<bool>> ReplyFunc { get; init; }
        public required IPAddress From { get; init; }
        public required byte[] Data {  get; init; }


        [SetsRequiredMembers]
        public DataInfo(IPAddress from, byte[] data, Func<byte[], Task<bool>> replyFunc)
        {
            From = from;
            Data = data;
            ReplyFunc = replyFunc;
        }
    }
}
