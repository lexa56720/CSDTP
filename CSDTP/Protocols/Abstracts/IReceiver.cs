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
    public interface IReceiver : IDisposable
    {
        public event EventHandler<(IPAddress from, byte[] data)> DataAppear;
        public bool IsReceiving { get; }
        public int Port { get; }

        public void Start();
        public void Stop();
    }
}
