using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using CSDTP.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    public class UdpResponderPipeline : ResponderPipeline
    {
        public override int Port => Receiver.Port;

        private QueueProcessorAsync<(IPAddress, byte[])> RequestsQueue { get; set; }

        private LifeTimeController<ISender> Senders { get; set; } = new(TimeSpan.FromSeconds(5));

        private IReceiver Receiver;

        public UdpResponderPipeline(int port, IEncryptProvider? encryptProvider = null, Type? customPacketType = null)
                                   : base(encryptProvider, customPacketType)
        {
            RequestsQueue = new(DataAppear, 5, TimeSpan.FromMilliseconds(20));
            Receiver = ReceiverFactory.CreateReceiver(port, Protocol.Udp);
            Receiver.DataAppear += (o, e) => RequestsQueue.Add(e);
        }
        public UdpResponderPipeline(IEncryptProvider? encryptProvider = null, Type? customPacketType = null)
                   : base(encryptProvider, customPacketType)
        {
            RequestsQueue = new(DataAppear, 5, TimeSpan.FromMilliseconds(20));
            Receiver = ReceiverFactory.CreateReceiver(Protocol.Udp);
            Receiver.DataAppear += (o, e) => RequestsQueue.Add(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    Stop();
                    Receiver.Dispose();
                }
                isDisposed = true;
            }
        }

        private async Task DataAppear((IPAddress from, byte[] data) request)
        {
            var (response, requestPacket) = HandleRequest(request.from, request.data);
            if (((IRequestContainer)requestPacket.DataObj).RequestKind == RequesKind.Data)
                return;

            if (!ResponseIfNull && response == null)
                return;

            var sender = Senders.Get(s => s.Destination.Equals(request.from) && s.IsAvailable);
            if (sender == null)
            {
                sender = SenderFactory.CreateSender(new IPEndPoint(request.from, requestPacket.ReplyPort), Protocol.Udp);
                Senders.Add(sender);
            }

            if (response == null)
                await sender.SendBytes([]);
            else
                await sender.SendBytes(response);
        }

        protected override void Start(bool isRunning)
        {
            Receiver.Start();
            RequestsQueue.Start();
            Senders.Start();
        }

        protected override void Stop(bool isRunning)
        {
            Receiver.Stop();
            RequestsQueue.Stop();
            Senders.Stop();
        }
    }
}
