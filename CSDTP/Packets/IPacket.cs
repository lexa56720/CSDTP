using CSDTP.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Packets
{

    public interface IPacket
    {
        public Type TypeOfPacket { get; }


        public CryptMethod CryptMethod { get; }

        public object DataObj { get; }

        public IPAddress Source { get; set; }

        public IPacket Deserialize(BinaryReader reader, IEncryptProvider encryptProvider);
        public IPacket Deserialize(BinaryReader reader);

        public int ReplyPort { get; }

        public DateTime SendTime { get; }

        public DateTime ReceiveTime { get; set; }
    }
}
