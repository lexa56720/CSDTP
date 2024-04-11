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
                await Utils.PortUtils.ModifyHttpSettings(Port, false);
        }
        public override async ValueTask Start()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                await Utils.PortUtils.ModifyHttpSettings(Port, true);
            Listener.Start();
            await base.Start();
        }

        protected override async Task ReceiveWork(CancellationToken token)
        {
            try
            {
                while (IsReceiving)
                {
                    await Listener.GetContextAsync().ContinueWith(HandleRequest, token, token);
                }
            }
            catch
            {
            }
            Listener.Stop();
        }
        private async Task HandleRequest(Task<HttpListenerContext> contextTask, object? state)
        {
            if (state is not CancellationToken token)
                return;

            var data = await contextTask.WaitAsync(token);

            var bytes = await ReadBytes(data, token);

            token.ThrowIfCancellationRequested();
            var address = data.Request.RemoteEndPoint.Address.GetAddressBytes();
            data.Response.StatusCode = (int)HttpStatusCode.OK;
            data.Response.Close();

            OnDataAppear(bytes, new IPAddress(address));
        }

        private async Task<byte[]> ReadBytes(HttpListenerContext context, CancellationToken token)
        {
            var bytes = new byte[context.Request.ContentLength64];
            var ms = new MemoryStream(bytes,true);
            await context.Request.InputStream.CopyToAsync(ms,token);
            return bytes;
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
