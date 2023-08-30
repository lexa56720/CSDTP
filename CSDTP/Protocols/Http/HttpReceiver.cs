using CSDTP.Cryptography.Providers;
using CSDTP.Protocols.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Http
{
    internal class HttpReceiver : BaseReceiver
    {
        private CancellationTokenSource TokenSource = new CancellationTokenSource();

        private HttpListener Listener;

        public override int Port => new Uri(Listener.Prefixes.First()).Port;
        public HttpReceiver()
        {
            Listener = new HttpListener();
        }

        public HttpReceiver(int port) : base(port)
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://{IPAddress.Loopback}:{port}/");
        }

        public HttpReceiver(IEncryptProvider decrypter) : base(decrypter)
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://{IPAddress.Loopback}:{Utils.PortUtils.GetFreePort()}/");
        }

        public HttpReceiver(int port, IEncryptProvider decryptProvider) : base(port, decryptProvider)
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://{IPAddress.Loopback}:{port}/");
        }

        public override void Dispose()
        {
            Stop();
            TokenSource.Cancel();
            TokenSource.Dispose();
        }
        public override void Start()
        {
            base.Start();
            Listener.Start();

            TokenSource = new CancellationTokenSource();
            var token = TokenSource.Token;

            Task.Run(async () =>
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
                    catch (OperationCanceledException e)
                    {
                        return;
                    }

                }
                Listener.Stop();
                Listener.Close();
            }, token);
        }


        public override void Stop()
        {
            base.Stop();          
        }
        private async Task<byte[]> ReadBytes(HttpListenerContext context, CancellationToken token)
        {
            var bytes = new byte[context.Request.ContentLength64];
            await context.Request.InputStream.ReadExactlyAsync(bytes, token);
            return bytes;
        }

    }
}
