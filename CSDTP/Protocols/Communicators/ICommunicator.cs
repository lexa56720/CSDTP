using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Communicators
{
    internal interface ICommunicator : IDisposable
    {
        public bool IsReceiving { get; }
        public bool IsAvailable { get; }

        public int ListenPort { get; }

        public IPEndPoint? Destination { get; }

        public event EventHandler<DataInfo>? DataAppear;

        public Task<bool> SendBytes(byte[] bytes);
        public Task<bool> SendBytes(byte[] bytes, IPEndPoint destination);

        public ValueTask Start();

        public ValueTask Stop();
    }
}
