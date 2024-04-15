using CSDTP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Communicators
{
    internal class HttpCommunicator : ICommunicator
    {
        public bool IsReceiving { get; private set; } = false;

        public bool IsAvailable { get; private set; } = true;


        public int ListenPort { get; }

        public event EventHandler<DataInfo>? DataAppear;

        private readonly HttpListener Listener;
        private readonly HttpClient Client;
        private CancellationTokenSource? TokenSource;
        public IPEndPoint? Destination { get; }

        private bool IsDisposed;
        private bool IsSending;

        public HttpCommunicator(int port, IPEndPoint? destination = null)
        {
            Listener = new HttpListener();
            Destination = destination;
            ListenPort = port;
            Listener.Prefixes.Add($"http://+:{ListenPort}/");
            Client = new HttpClient();
        }
        public HttpCommunicator(IPEndPoint? destination = null)
        {
            Listener = new HttpListener();
            Destination = destination;
            ListenPort = PortUtils.GetFreePort(6660);
            Listener.Prefixes.Add($"http://+:{ListenPort}/");
            Client = new HttpClient();
        }
        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            IsAvailable = false;
            if (!IsSending)
                Client.Dispose();
            Stop();
        }
        public async Task<bool> SendBytes(byte[] bytes)
        {
            if (Destination == null)
                return false;
            return await SendBytes(bytes, Destination);
        }
        public async Task<bool> SendBytes(byte[] bytes, IPEndPoint destination)
        {
            if (!IsAvailable)
                return false;

            if (TokenSource == null || TokenSource.IsCancellationRequested)
                TokenSource = new CancellationTokenSource();

            try
            {
                IsSending = true;
                using var response = await Client.SendAsync(new HttpRequestMessage()
                {
                    Content = new ByteArrayContent(bytes),
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"http://{destination.Address}:{destination.Port}/")
                }, HttpCompletionOption.ResponseContentRead, TokenSource.Token);
                if (response.IsSuccessStatusCode)
                    await OnDataAppear(response, destination);
                IsSending = false;
                if (IsDisposed)
                    Client.Dispose();
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async ValueTask Start()
        {
            if (IsReceiving || IsDisposed)
                return;
            IsReceiving = true;
            if (TokenSource == null || TokenSource.IsCancellationRequested)
                TokenSource = new CancellationTokenSource();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                await PortUtils.ModifyHttpSettings(ListenPort, true);

            Listener.Start();
            ReceiveWork(TokenSource.Token);
        }
        public async ValueTask Stop()
        {
            if (!IsReceiving)
                return;
            IsReceiving = false;
            TokenSource?.Cancel();
            TokenSource?.Dispose();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                await PortUtils.ModifyHttpSettings(ListenPort, false);
        }

        private async Task ReceiveWork(CancellationToken token)
        {
            while (IsReceiving)
            {
                try
                {
                    var context = await Listener.GetContextAsync().WaitAsync(token);
                    await HandleRequest(context, token);
                }
                catch
                {
                    if (IsDisposed)
                        break;
                }
            }
            try
            {
                Listener.Stop();
            }
            catch
            {

            }
        }

        private async Task HandleRequest(HttpListenerContext context, CancellationToken token)
        {
            var bytes = await ReadBytes(context, token);

            token.ThrowIfCancellationRequested();
            OnDataAppear(bytes, context.Request.RemoteEndPoint, context);
        }
        private async Task<byte[]> ReadBytes(HttpListenerContext context, CancellationToken token)
        {
            var bytes = new byte[context.Request.ContentLength64];
            using var ms = new MemoryStream(bytes, true);
            await context.Request.InputStream.CopyToAsync(ms, token);
            return bytes;
        }

        private async Task OnDataAppear(HttpResponseMessage response, IPEndPoint destination)
        {
            var resposeBytes = await response.Content.ReadAsByteArrayAsync();
            Func<byte[], Task<bool>> replyFunc = (data) =>
            {
                return Task.FromResult(false);
            };
            DataAppear?.Invoke(this, new DataInfo(destination.Address, resposeBytes, replyFunc));
        }
        private void OnDataAppear(byte[] buffer, IPEndPoint endPoint, HttpListenerContext context)
        {
            Func<byte[], Task<bool>> replyFunc = async (data) =>
            {
                return await Reply(data, context);
            };
            DataAppear?.Invoke(this, new DataInfo(endPoint.Address, buffer, replyFunc));
        }

        private async Task<bool> Reply(byte[] data, HttpListenerContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.OutputStream.WriteAsync(data);
            context.Response.Close();
            return true;
        }
    }
}
