using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    public class Requester
    {
        private ISender Sender { get; set; }
        private IReceiver Receiver { get; set; }

        public ConcurrentDictionary<Guid, TaskCompletionSource<IPacket>> Requests = new ConcurrentDictionary<Guid, TaskCompletionSource<IPacket>>();

        public Requester(IPEndPoint destination, int replyPort, bool isTcp = false)
        {
            Sender = new Sender(destination, isTcp, replyPort);
            Receiver = new Receiver(ReplyPort, isTcp);
            Receiver.DataAppear += ResponseAppear;
        }

        private void ResponseAppear(object? sender, IPacket e)
        {
            var packet = (IRequestContainer)e.DataObj;
            if (Requests.TryGetValue(packet.Id, out var request))
            {
                request.SetResult(e);
            }

        }

        public async Task<T> PostAsync<T, U>(U data, TimeSpan timeout) where U : ISerializable<U> where T : ISerializable<T>
        {
            var container = new RequestContainer<U>(data, RequestType.Post);
            await Sender.Send(container);

            var resultSource = new TaskCompletionSource<IPacket>();

            if (Requests.TryAdd(container.Id, resultSource))
            {
                var task = Task.Delay((int)timeout.TotalMilliseconds);
                var response = resultSource.Task;

                Task.WaitAny(task, response);

                if (response.IsCompletedSuccessfully)
                    return ((Packet<RequestContainer<T>>)response.Result).Data.Data;
            }

            throw new Exception("Request sending error");
        }


        public async Task<bool> GetAsync<U>(U data) where U : ISerializable<U>
        {
            var container = new RequestContainer<U>(data, RequestType.Get);
            return await Sender.Send(container);
        }
        public IPEndPoint Destination => Sender.Destination;
        public int ReplyPort => Sender.ReplyPort;
    }
}
