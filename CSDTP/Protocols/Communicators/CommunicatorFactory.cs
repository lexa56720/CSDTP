using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Communicators
{
    internal class CommunicatorFactory
    {

        public static ICommunicator Create(IPEndPoint destination, int listenPort, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    return new UdpCommunicator(listenPort, destination);
                case Protocol.Http:
                    return new HttpCommunicator(listenPort, destination);
            }
            throw new NotImplementedException("PROTOCOL NOT IMPLEMENTED");
        }
        public static ICommunicator Create(IPEndPoint destination, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    return new UdpCommunicator(destination);
                case Protocol.Http:
                    return new HttpCommunicator(destination);
            }
            throw new NotImplementedException("PROTOCOL NOT IMPLEMENTED");
        }


        public static ICommunicator Create(int listenPort, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    return new UdpCommunicator(listenPort);
                case Protocol.Http:
                    return new HttpCommunicator(listenPort);
            }
            throw new NotImplementedException("PROTOCOL NOT IMPLEMENTED");
        }
        public static ICommunicator Create(Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.Udp:
                    return new UdpCommunicator();
                case Protocol.Http:
                    return new HttpCommunicator();
            }
            throw new NotImplementedException("PROTOCOL NOT IMPLEMENTED");
        }
    }
}
