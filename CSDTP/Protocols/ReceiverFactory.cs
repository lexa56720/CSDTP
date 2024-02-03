using CSDTP.Protocols.Abstracts;
using CSDTP.Protocols.Http;
using CSDTP.Protocols.Udp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols
{
    public class ReceiverFactory
    {

        public static IReceiver CreateReceiver(int listenPort, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    return new UdpReceiver(listenPort);
                case Protocol.Http:
                    return new HttpReceiver(listenPort);
            }
            throw new NotImplementedException("PROTOCOL NOT IMPLEMENTED");
        }

        public static IReceiver CreateReceiver(Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    return new UdpReceiver();
                case Protocol.Http:
                    return new HttpReceiver();
            }
            throw new NotImplementedException("PROTOCOL NOT IMPLEMENTED");
        }

    }
}
