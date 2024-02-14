using System.Net;

namespace CSDTP.Packets
{
    public interface IPacketInfo
    {
        public IPAddress? Source { get; set; }

        public int ReplyPort { get; }

        public DateTime SendTime { get; }

        public DateTime ReceiveTime { get; set; }
    }
}
