using CSDTP.Cryptography.Providers;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Utils.Collections;
using System.Net;

namespace CSDTP.Requests
{
    internal class HttpResponder : Responder
    {
        private LifeTimeController<ISender> Senders { get; set; } = new(TimeSpan.FromMinutes(5));
        public override Protocol Protocol => Protocol.Http;

        internal HttpResponder(IEncryptProvider? encryptProvider = null, Type? customPacketType = null) :
            base(ReceiverFactory.CreateReceiver(Protocol.Http), encryptProvider, customPacketType)
        {
        }
        internal HttpResponder(int port, IEncryptProvider? encryptProvider = null, Type? customPacketType = null) :
            base(ReceiverFactory.CreateReceiver(port, Protocol.Http), encryptProvider, customPacketType)
        {
        }


        protected override ISender GetSender(IPEndPoint endPoint)
        {
            var sender = Senders.Get(s => s.IsAvailable && s.Destination.Equals(endPoint));
            if (sender == null)
            {
                sender = SenderFactory.CreateSender(endPoint, Protocol);
                Senders.Add(sender);
            }
            return sender;
        }

        protected override void Dispose(bool disposing)
        {
            Senders.Clear();
        }

        protected override void Start(bool isRunning)
        {
            Senders.Start();
        }

        protected override void Stop(bool isRunning)
        {
            Senders.Stop();
        }
    }
}
