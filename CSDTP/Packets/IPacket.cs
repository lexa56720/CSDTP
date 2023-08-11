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

        public IPacket Deserialize(BinaryReader reader);
    }
}
