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

    public interface IPacket : IPacketInfo
    {
        public Type TypeOfPacket { get; }

        public object? DataObj { get; }


        public void Serialize(BinaryWriter writer);

        public void Serialize(BinaryWriter writer, IEncryptProvider encryptProvider);

        public IPacket Deserialize(BinaryReader reader, IEncryptProvider encryptProvider);
        public IPacket Deserialize(BinaryReader reader);

    }
}
