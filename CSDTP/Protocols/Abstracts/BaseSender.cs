using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Abstracts
{
    internal abstract class BaseSender :ISender
    {
        public IPEndPoint Destination { get; }

        public bool IsAvailable { get; protected set; } = true;

        public BaseSender(IPEndPoint destination)
        {
            Destination = destination;
        }

        public abstract void Dispose();

        public abstract Task<bool> SendBytes(byte[] bytes);
    }
}
