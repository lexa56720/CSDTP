using Open.Nat;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CSDTP.Utils
{
    public static class PortUtils
    {
        private readonly static Random Random = new Random();
        public static int GetFreePort(int startingPort = 1)
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            var tcpConnectionPorts = properties.GetActiveTcpConnections()
                                .Where(n => n.LocalEndPoint.Port >= startingPort)
                                .Select(n => n.LocalEndPoint.Port);

            var tcpListenerPorts = properties.GetActiveTcpListeners()
                                .Where(n => n.Port >= startingPort)
                                .Select(n => n.Port);

            var udpListenerPorts = properties.GetActiveUdpListeners()
                                .Where(n => n.Port >= startingPort)
                                .Select(n => n.Port);

            var ports = Enumerable.Range(startingPort, ushort.MaxValue - startingPort)
                                  .Where(i => !tcpConnectionPorts.Contains(i))
                                  .Where(i => !tcpListenerPorts.Contains(i))
                                  .Where(i => !udpListenerPorts.Contains(i));

            return ports.ElementAt(Random.Next(0, ports.Count()));
        }

        public static async Task<bool> PortForward(int port, string mappingName, bool isTcp = false)
        {
            try
            {
                var discoverer = new NatDiscoverer();
                var cts = new CancellationTokenSource(10000);
                var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
                var mapping = new Mapping(isTcp ? Protocol.Tcp : Protocol.Udp, port, port, mappingName);
                await device.CreatePortMapAsync(mapping);
            }
            catch
            {
                return false;
            }
            return true;
        }

        [SupportedOSPlatform("windows")]
        public static async Task ModifyHttpSettings(int port, bool isAdd)
        {
            string everyone = new System.Security.Principal.SecurityIdentifier("S-1-1-0")
                                 .Translate(typeof(System.Security.Principal.NTAccount))
                                 .ToString();
            var command = isAdd ? "add" : "delete";
            string parameter = $"http {command} urlacl url=http://+:{port}/ user=\\{everyone}";

            var procInfo = new ProcessStartInfo("netsh", parameter)
            {
                Verb = "runas",
                RedirectStandardOutput = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };
            var proc = Process.Start(procInfo);
            if (proc != null)
                await proc.WaitForExitAsync();
        }

        public static async Task<bool> PortBackward(int port, string name, bool isTcp = false)
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
