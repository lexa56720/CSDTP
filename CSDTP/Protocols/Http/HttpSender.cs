using CSDTP.Protocols.Abstracts;
using System.Net;

namespace CSDTP.Protocols.Http
{
    internal class HttpSender : BaseSender
    {
        private HttpClient HttpClient { get; set; }
        private CancellationTokenSource CancellationToken { get; set; } = new CancellationTokenSource();
        public HttpSender(IPEndPoint destination) : base(destination)
        {
            HttpClient = new HttpClient
            {
                BaseAddress = new UriBuilder(Destination.ToString()).Uri,
                DefaultRequestVersion = HttpVersion.Version30,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            };
        }

        public override void Dispose()
        {
            IsAvailable = false;
            CancellationToken.Cancel();
            CancellationToken.Dispose();
            HttpClient.Dispose();
        }

        public override async Task<bool> SendBytes(byte[] bytes)
        {
            if (!IsAvailable)
                return false;

            try
            {
                using var response = await HttpClient.SendAsync(new HttpRequestMessage()
                {
                    Content = new ByteArrayContent(bytes),
                    Method = HttpMethod.Post,

                }, HttpCompletionOption.ResponseHeadersRead, CancellationToken.Token);
                return response.IsSuccessStatusCode;
            }

            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }

}
