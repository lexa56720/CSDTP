using Open.Nat;
using System.Net.NetworkInformation;

namespace CSDTP.Utils
{
    public static class PortUtils
    {
        public static int GetFreePort(int startingPort = 1)
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            //getting active connections
            var tcpConnectionPorts = properties.GetActiveTcpConnections()
                                .Where(n => n.LocalEndPoint.Port >= startingPort)
                                .Select(n => n.LocalEndPoint.Port);

            //getting active tcp listners - WCF service listening in tcp
            var tcpListenerPorts = properties.GetActiveTcpListeners()
                                .Where(n => n.Port >= startingPort)
                                .Select(n => n.Port);

            //getting active udp listeners
            var udpListenerPorts = properties.GetActiveUdpListeners()
                                .Where(n => n.Port >= startingPort)
                                .Select(n => n.Port);

            var port = Enumerable.Range(startingPort, ushort.MaxValue)
                .Where(i => !tcpConnectionPorts.Contains(i))
                .Where(i => !tcpListenerPorts.Contains(i))
                .Where(i => !udpListenerPorts.Contains(i))
                .FirstOrDefault();

            return port;
        }

        public static async Task<bool> PortForward(int port, string mappingName, bool isTcp = false)
        {
            try
            {
                var discoverer = new NatDiscoverer();
                var cts = new CancellationTokenSource(10000);
                var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);


                await device.CreatePortMapAsync(new Mapping(isTcp ? Protocol.Tcp : Protocol.Udp, port, port, mappingName));
                return true;
            }
            catch
            {
                return false;
            }

        }


        public static async Task<bool> PortBackward(int port, bool isTcp = false)
        {
            try
            {
                var discoverer = new NatDiscoverer();
                var cts = new CancellationTokenSource(10000);
                var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

                await device.DeletePortMapAsync(await device.GetSpecificMappingAsync(isTcp ? Protocol.Tcp : Protocol.Udp, port));
                return true;
            }
            catch
            {
                return false;
            }

        }
    }
}
