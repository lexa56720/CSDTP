using CSDTP.Cryptography.Providers;
using CSDTP.Protocols.Abstracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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

        public override void Stop()
        {
            base.Stop();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                ModifyHttpSettings(Port, false);
        }
        public override void Start()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                ModifyHttpSettings(Port, true);
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
                    using Stream output = data.Response.OutputStream;
                    await output.FlushAsync(token);
                    token.ThrowIfCancellationRequested();

                    ReceiverQueue.Add((await ReadBytes(data, token), data.Request.RemoteEndPoint.Address));
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

        [SupportedOSPlatform("windows")]
        private void ModifyHttpSettings(int port, bool isAdd)
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
            proc.WaitForExitAsync();
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
