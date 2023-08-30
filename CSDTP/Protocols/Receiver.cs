using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols.Abstracts;
using CSDTP.Protocols.Http;
using CSDTP.Protocols.Udp;
using Open.Nat;
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

        public Receiver(int port, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    ReceiverSocket = new UdpReceiver(port);
                    break;
                case Protocol.Tcp:
                    throw new NotImplementedException("TCP NOT IMPLEMENTED");
                    break;
                case Protocol.Http:
                    ReceiverSocket = new HttpReceiver(port);
                    break;
            }
        }
        public Receiver(int port, IEncryptProvider encryptProvider, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    ReceiverSocket = new UdpReceiver(port, encryptProvider);
                    break;
                case Protocol.Tcp:
                    throw new NotImplementedException("TCP NOT IMPLEMENTED");
                    break;
                case Protocol.Http:
                    ReceiverSocket = new HttpReceiver(port, encryptProvider);
                    break;
            }
        }
        public Receiver(Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    ReceiverSocket = new UdpReceiver();
                    break;
                case Protocol.Tcp:
                    throw new NotImplementedException("TCP NOT IMPLEMENTED");
                    break;
                case Protocol.Http:
                    ReceiverSocket = new HttpReceiver();
                    break;
            }
        }
        public Receiver(IEncryptProvider encryptProvider, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    ReceiverSocket = new UdpReceiver(encryptProvider);
                    break;
                case Protocol.Tcp:
                    throw new NotImplementedException("TCP NOT IMPLEMENTED");
                    break;
                case Protocol.Http:
                    ReceiverSocket = new HttpReceiver(encryptProvider);
                    break;
            }
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
    }
}
