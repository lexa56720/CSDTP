using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Packets
{
    public interface IPacket
    {

        public Type TypeOfPacket { get; }

        public object Data { get; }

        public IPacket Deserialize(BinaryReader reader);

        public int ReplyPort { get; init; }

        public DateTime SendTime { get; init; }

        public DateTime ReceiveTime { get; set; }
    }
}
