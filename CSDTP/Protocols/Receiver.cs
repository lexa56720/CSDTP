using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols.Abstracts;
using CSDTP.Protocols.Udp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols
{
    public class Receiver : IReceiver
    {

        private BaseReceiver ReceiverSocket;


        public bool IsReceiving { get => ReceiverSocket.IsReceiving; }

        public int Port => ReceiverSocket.Port;

        public IEncryptProvider? DecryptProvider 
        { 
            get => ReceiverSocket.DecryptProvider;
            set => ReceiverSocket.DecryptProvider = value;
        }

        public Receiver(int port, bool isTcp = false)
        {
            if (isTcp)
                throw new NotImplementedException("TCP NOT IMPLEMENTED");
            else
                ReceiverSocket = new UdpReceiver(port);
        }

        public Receiver(int port, IEncryptProvider encryptProvider, bool isTcp = false)
        {
            if (isTcp)
                throw new NotImplementedException("TCP NOT IMPLEMENTED");
            else
                ReceiverSocket = new UdpReceiver(port, encryptProvider);
        }
        public Receiver(bool isTcp = false)
        {
            if (isTcp)
                throw new NotImplementedException("TCP NOT IMPLEMENTED");
            else
                ReceiverSocket = new UdpReceiver();

        }

        public Receiver(IEncryptProvider encryptProvider, bool isTcp = false)
        {
            if (isTcp)
                throw new NotImplementedException("TCP NOT IMPLEMENTED");
            else
                ReceiverSocket = new UdpReceiver(encryptProvider);
        }

        public event EventHandler<IPacket> DataAppear
        {
            add
            {
                ((IReceiver)ReceiverSocket).DataAppear += value;
            }

            remove
            {
                ((IReceiver)ReceiverSocket).DataAppear -= value;
            }
        }

        public void Dispose()
        {
            ReceiverSocket.Dispose();
        }

        public void Start()
        {
            ReceiverSocket.Start();
        }

        public void Stop()
        {
            ReceiverSocket.Stop();
        }

        public void Close()
        {
            ReceiverSocket.Close();
        }
    }
}
