using CSDTP.Cryptography;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CSDTP.Requests
{
    public class Requester : IDisposable
    {
        private ISender Sender { get; set; }
        private IReceiver Receiver { get; set; }

        private IEncrypter? Encrypter { get; init; }

        public bool IsAvailable => Sender.IsAvailable && Receiver.IsReceiving;

        public ConcurrentDictionary<Guid, TaskCompletionSource<IPacket>> Requests = new ConcurrentDictionary<Guid, TaskCompletionSource<IPacket>>();

        public Requester(IPEndPoint destination, int replyPort, bool isTcp = false)
        {
            Sender = new Sender(destination, isTcp, replyPort);

            Receiver = new Receiver(ReplyPort, isTcp);
            Receiver.DataAppear += ResponseAppear;
            Receiver.Start();
        }
        public Requester(IPEndPoint destination, int replyPort, IEncrypter encrypter, bool isTcp = false)
        {
            Encrypter = encrypter;
            Sender = new Sender(destination, isTcp, replyPort);

            Receiver = new Receiver(ReplyPort, encrypter, isTcp);
            Receiver.DataAppear += ResponseAppear;
            Receiver.Start();
        }

        private void ResponseAppear(object? sender, IPacket e)
        {
            var packet = (IRequestContainer)e.DataObj;
            if (Requests.TryGetValue(packet.Id, out var request))
                request.SetResult(e);
        }

        public async Task<T> PostAsync<T, U>(U data, TimeSpan timeout) where U : ISerializable<U> where T : ISerializable<T>
        {
            var container = new RequestContainer<U>(data, RequestType.Post);
            await Send(container);

            return await GetResponse<T, U>(container, timeout);
        }


        private async Task<T> GetResponse<T, U>(RequestContainer<U> container, TimeSpan timeout) where U : ISerializable<U> where T : ISerializable<T>
        {
            var resultSource = new TaskCompletionSource<IPacket>();

            if (Sender.IsAvailable && Requests.TryAdd(container.Id, resultSource))
            {
                var response = resultSource.Task;
                await response.WaitAsync(timeout);

                Requests.TryRemove(new KeyValuePair<Guid, TaskCompletionSource<IPacket>>(container.Id, resultSource));
                if (response.IsCompletedSuccessfully)
                    return ((Packet<RequestContainer<T>>)response.Result).Data.Data;
            }

            throw new Exception("Request sending error");
        }


        public async Task<bool> GetAsync<U>(U data) where U : ISerializable<U>
        {
            if (!Sender.IsAvailable)
                throw new Exception("Request sending error");

            var container = new RequestContainer<U>(data, RequestType.Get);
            return await Send(container);
        }


        private async Task<bool> Send<T>(T container) where T : ISerializable<T>
        {
            if (Encrypter == null)
                return await Sender.Send(container);
            else
                return await Sender.Send(container, Encrypter);
        }

        public void Dispose()
        {
            Sender.Dispose();
            Receiver.DataAppear -= ResponseAppear;
            Receiver.Dispose();
            Encrypter?.Dispose();
        }

        public IPEndPoint Destination => Sender.Destination;
        public int ReplyPort => Sender.ReplyPort;
    }
}
