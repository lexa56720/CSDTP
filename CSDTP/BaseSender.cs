using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP
{
    internal abstract class BaseSender: IDisposable 
    {
        public IPEndPoint Destination { get; }
        public int ReplyPort { get; }

        public BaseSender(IPEndPoint destination,int replyPort=-1)
        {
            Destination = destination;
            ReplyPort = replyPort;
        }

        public abstract void Dispose();

        public abstract Task<bool> Send<T>(T data) where T : ISerializable<T>;


        protected Packet<T> GetPacket<T>(T data) where T : ISerializable<T>
        {
            return new Packet<T>(data);
        }

    }
}
