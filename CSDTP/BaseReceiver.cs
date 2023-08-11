using CSDTP.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP
{
    internal abstract class BaseReceiver : IDisposable
    {
        public bool IsReceiving { get; protected set; }
        public int Port { get; }

        public event EventHandler<IPacket> DataAppear;

        public BaseReceiver(int port)
        {
            Port = port;
        }

        public abstract void Dispose();

        public virtual void Start()
        {
            IsReceiving = true;
        }

        public virtual void Stop()
        {
            IsReceiving = false;
        }

        protected virtual void OnDataAppear(IPacket packet)
        { 
            DataAppear?.Invoke(this, packet);
        }
    }
}
