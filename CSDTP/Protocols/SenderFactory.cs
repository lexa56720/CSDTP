using CSDTP.DosProtect;
using CSDTP.Protocols.Abstracts;
using CSDTP.Protocols.Http;
using CSDTP.Protocols.Udp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols
{
    public class SenderFactory
    {

        public static ISender CreateSender(IPEndPoint destination, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    return new UdpSender(destination);
                case Protocol.Http:
                    return new HttpSender(destination);
            }
            throw new NotImplementedException("PROTOCOL NOT IMPLEMENTED");
        }
    }
}
