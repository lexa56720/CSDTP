using CSDTP.Protocols.Abstracts;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace CSDTP.Protocols.Http
{
    internal class HttpReceiver : BaseReceiver
    {

        private readonly HttpListener Listener;

        public override int Port => GetPortFromUrl(Listener.Prefixes.First());
        public HttpReceiver()
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://+:{Utils.PortUtils.GetFreePort(666)}/");

        }

        public HttpReceiver(int port) : base(port)
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://+:{port}/");
        }

        public override async ValueTask Stop()
        {
            await base.Stop();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                await ModifyHttpSettings(Port, false);
        }
        public override async ValueTask Start()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                await ModifyHttpSettings(Port, true);
            Listener.Start();
            await base.Start();
        }

        protected override async Task ReceiveWork(CancellationToken token)
        {
            try
            {
                await Task.Run(() =>
                {

                    while (IsReceiving)
                    {
                        var result = Listener.BeginGetContext((o) => ListenerCallback(o, token), Listener);
                        result.AsyncWaitHandle.WaitOne();
                    }

                }, token);
            }
            catch (OperationCanceledException)
            {
                Listener.Abort();
                return;
            }
        }
        private void ListenerCallback(IAsyncResult result, CancellationToken token)
        {

            var listener = (HttpListener)result.AsyncState;

            var data = listener.EndGetContext(result);

            if (token.IsCancellationRequested)
                return;

            var bytes = ReadBytes(data, token);

            if (token.IsCancellationRequested)
                return;

            OnDataAppear(bytes, data.Request.RemoteEndPoint.Address);

            data.Response.StatusCode = (int)HttpStatusCode.OK;
            data.Response.Close();
        }

        private byte[] ReadBytes(HttpListenerContext context, CancellationToken token)
        {
            var bytes = new byte[context.Request.ContentLength64];
            context.Request.InputStream.ReadExactly(bytes);
            return bytes;
        }

        [SupportedOSPlatform("windows")]
        private async Task ModifyHttpSettings(int port, bool isAdd)
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
        private int GetPortFromUrl(string url)
        {
            var startIndex = url.IndexOf(':', 5) + 1;
            var endIndex = url.IndexOf('/', startIndex);
            var port = url[startIndex..endIndex];
            return int.Parse(port);
        }
    }
}
