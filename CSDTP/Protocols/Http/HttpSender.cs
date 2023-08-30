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
    internal class HttpSender : BaseSender
    {
        private HttpClient HttpClient { get; set; }

        private CancellationTokenSource CancellationToken { get; set; } = new CancellationTokenSource();
        public HttpSender(IPEndPoint destination) : base(destination)
        {
            HttpClient = new HttpClient();
            HttpClient.BaseAddress = new UriBuilder(destination.ToString()).Uri;
        }

        public HttpSender(IPEndPoint destination, int replyPort = -1) : base(destination, replyPort)
        {
            HttpClient = new HttpClient();
            HttpClient.BaseAddress = new UriBuilder(destination.ToString()).Uri;
        }

        public HttpSender(IPEndPoint destination, IEncryptProvider encryptProvider) : base(destination, encryptProvider)
        {
            HttpClient = new HttpClient();
            HttpClient.BaseAddress = new UriBuilder(destination.ToString()).Uri;
        }

        public HttpSender(IPEndPoint destination, IEncryptProvider encryptProvider, int replyPort = -1) : base(destination, encryptProvider, replyPort)
        {
            HttpClient = new HttpClient();
            HttpClient.BaseAddress = new UriBuilder(destination.ToString()).Uri;
        }


        public override void Dispose()
        {
            CancellationToken.Cancel();
            CancellationToken.Dispose();
            HttpClient.Dispose();
            IsAvailable = false;
        }

        protected override async Task<bool> SendBytes(byte[] bytes)
        {
            if (!IsAvailable)
                return false;
            var response = await HttpClient.SendAsync(new HttpRequestMessage()
            {
                Content = new ByteArrayContent(bytes),
                Method = HttpMethod.Get
            }, CancellationToken.Token);
            return response.IsSuccessStatusCode;
        }
    }
}
