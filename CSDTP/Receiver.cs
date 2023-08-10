using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP
{
    internal  abstract class BaseReceiver : IDisposable
    {
        public int Port { get; }
        public BaseReceiver(int port) 
        {
            Port = port;
        }

        public abstract void Dispose();

        public abstract void Start();

        public abstract void Stop();
    }
}
