using CSDTP.Cryptography.Providers;
using CSDTP.Protocols.Abstracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Http
{
    internal class HttpReceiver : BaseReceiver
    {

        private HttpListener Listener;

        public override int Port => GetPortFromUrl(Listener.Prefixes.First());
        public HttpReceiver()
        {
            Listener = new HttpListener();
        }

        public HttpReceiver(int port) : base(port)
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://+:{port}/");
        }

        public HttpReceiver(IEncryptProvider decrypter) : base(decrypter)
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://+:{Utils.PortUtils.GetFreePort()}/");
        }

        public HttpReceiver(int port, IEncryptProvider decryptProvider) : base(port, decryptProvider)
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://+:{port}/");
        }

        public override void Dispose()
        {
            Stop();
            TokenSource.Cancel();
            TokenSource.Dispose();
        }
        public override void Stop()
        {
            base.Stop();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                ModifyHttpSettings(Port, false);
        }
        public override void Start()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                ModifyHttpSettings(Port,true);
            Listener.Start();
            base.Start();
        }

        protected override async Task ReceiveWork(CancellationToken token)
        {
            while (IsReceiving)
            {
                try
                {
                    var data = await Listener.GetContextAsync();

                    token.ThrowIfCancellationRequested();
                    if (IsAllowed(data.Request.RemoteEndPoint))
                    {
                        using Stream output = data.Response.OutputStream;
                        await output.FlushAsync(token);
                        token.ThrowIfCancellationRequested();

                        ReceiverQueue.Add(new Tuple<byte[], IPAddress>(
                            await ReadBytes(data, token),
                            data.Request.RemoteEndPoint.Address));
                    }

                }
                catch (OperationCanceledException)
                {
                    return;
                }

            }
            Listener.Stop();
            Listener.Close();
            ReceiverQueue.Clear();
        }


        private async Task<byte[]> ReadBytes(HttpListenerContext context, CancellationToken token)
        {
            var bytes = new byte[context.Request.ContentLength64];
            await context.Request.InputStream.ReadExactlyAsync(bytes, token);
            return bytes;
        }


        private void ModifyHttpSettings(int port,bool isAdd)
        {
            string everyone = new System.Security.Principal.SecurityIdentifier("S-1-1-0")
                .Translate(typeof(System.Security.Principal.NTAccount)).ToString();
            var command = isAdd? "add" : "delete";
            string parameter = $"http {command} urlacl url=http://+:{port}/ user=\\{everyone}";

            ProcessStartInfo psi = new ProcessStartInfo("netsh", parameter);

            psi.Verb = "runas";
            psi.RedirectStandardOutput = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = true;
            var proc = Process.Start(psi);
            proc.WaitForExit();
        }
        private int GetPortFromUrl(string url)
        {
            var startIndex = url.IndexOf(':', 5) + 1;
            var endIndex = url.IndexOf('/', startIndex);
            var port = url[startIndex..endIndex];
            return int.Parse(port);
        }
    }
}
