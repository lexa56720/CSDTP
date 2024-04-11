using CSDTP.Cryptography.Providers;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using PerformanceUtils.Collections;
using System.Net;

namespace CSDTP.Requests
{
    internal class UdpResponder : Responder
    {
        private LifeTimeDictionary<IPEndPoint, ISender> Senders { get; set; } = new((s) => s?.Dispose());

        private readonly TimeSpan SenderLifeTime = TimeSpan.FromSeconds(60);
        public override Protocol Protocol => Protocol.Udp;

        internal UdpResponder(IEncryptProvider? encryptProvider = null, Type? customPacketType = null) :
            base(ReceiverFactory.CreateReceiver(Protocol.Udp), encryptProvider, customPacketType)
        {
        }
        internal UdpResponder(int port, IEncryptProvider? encryptProvider = null, Type? customPacketType = null) :
            base(ReceiverFactory.CreateReceiver(port, Protocol.Udp), encryptProvider, customPacketType)
        {
        }


        protected override ISender GetSender(IPEndPoint endPoint)
        {
            if (!Senders.TryGetValue(endPoint, out var sender))
            {
                sender = SenderFactory.CreateSender(endPoint, Protocol);
                Senders.TryAdd(endPoint, sender, SenderLifeTime);
            }
            else
            {
                if (!sender.IsAvailable)
                {
                    Senders.TryRemove(endPoint, out _);
                    sender = SenderFactory.CreateSender(endPoint, Protocol);
                    Senders.TryAdd(endPoint, sender, SenderLifeTime);
                }
                else
                    Senders.UpdateLifetime(endPoint, SenderLifeTime);
            }
            return sender;
        }

        protected override void Dispose(bool disposing)
        {
            Senders.Clear();
        }
    }
}
