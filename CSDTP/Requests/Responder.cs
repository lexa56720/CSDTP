using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    internal class Responder
    {
        private ISender Sender { get; set; }
        private IReceiver Receiver { get; set; }

        public Responder(IPEndPoint destination, bool isTcp = false)
        {
            Sender = new Sender(destination, isTcp);
            Receiver = new Receiver(PortUtils.GetPort(), isTcp);
            Receiver.DataAppear += ResponseAppear;
        }

        private void ResponseAppear(object? sender, IPacket e)
        {
            throw new NotImplementedException();
        }
    }
}
