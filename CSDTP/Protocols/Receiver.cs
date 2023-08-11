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

        public Receiver(int port, bool isTcp = false)
        {
            if (isTcp)
            {

            }
            else
            {
                ReceiverSocket = new UdpReceiver(port);
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
