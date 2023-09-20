using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using CSDTP.Utils.Performance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
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


        private Type? PacketType = null;
        private IEncryptProvider EncryptProvider { get; init; }

        public bool IsAvailable => Sender.IsAvailable && Receiver.IsReceiving;
        public IPEndPoint Destination => Sender.Destination;
        public int ReplyPort => Sender.ReplyPort;

        private CompiledMethod SendCustomPacket;


        public ConcurrentDictionary<Guid, TaskCompletionSource<IPacket>> Requests = new ConcurrentDictionary<Guid, TaskCompletionSource<IPacket>>();

        public Requester(IPEndPoint destination, int replyPort, Protocol protocol=Protocol.Udp)
        {
            Sender = new Sender(destination, replyPort, protocol);

            Receiver = new Receiver(ReplyPort, protocol);
            Receiver.DataAppear += ResponseAppear;
            Receiver.Start();
            SetupMethods();
        }
        public Requester(IPEndPoint destination, int replyPort, IEncryptProvider encryptProvider, Protocol protocol = Protocol.Udp)
        {
            EncryptProvider = encryptProvider;


            Receiver = new Receiver(ReplyPort, protocol);
            Receiver.DataAppear += ResponseAppear;
            Receiver.Start();
            Sender = new Sender(destination, replyPort, protocol);
            SetupMethods();
        }
        public Requester(IPEndPoint destination, int replyPort, IEncryptProvider encryptProvider, IEncryptProvider decrypter, Protocol protocol = Protocol.Udp)
        {
            EncryptProvider = encryptProvider;


            Receiver = new Receiver(ReplyPort, decrypter, protocol);
            Receiver.DataAppear += ResponseAppear;
            Receiver.Start();

            Sender = new Sender(destination, encryptProvider, replyPort, protocol);
            SetupMethods();
        }
        public Requester(IPEndPoint destination, Protocol protocol = Protocol.Udp)
        {
            Receiver = new Receiver(protocol);
            Receiver.DataAppear += ResponseAppear;
            Receiver.Start();

            Sender = new Sender(destination, Receiver.Port, protocol);
            SetupMethods();
        }
        public Requester(IPEndPoint destination, IEncryptProvider encryptProvider, Protocol protocol = Protocol.Udp)
        {
            EncryptProvider = encryptProvider;

            Receiver = new Receiver(protocol);
            Receiver.DataAppear += ResponseAppear;
            Receiver.Start();

            Sender = new Sender(destination, encryptProvider, Receiver.Port, protocol);
            SetupMethods();
        }
        public Requester(IPEndPoint destination, IEncryptProvider encryptProvider, IEncryptProvider decryptProvider, Protocol protocol = Protocol.Udp)
        {
            EncryptProvider = encryptProvider;

            Receiver = new Receiver(decryptProvider, protocol);
            Receiver.DataAppear += ResponseAppear;
            Receiver.Start();

            Sender = new Sender(destination, encryptProvider, Receiver.Port, protocol);
            SetupMethods();
        }

        public void Dispose()
        {
            Sender.Dispose();
            Receiver.DataAppear -= ResponseAppear;
            Receiver.Dispose();
            EncryptProvider?.Dispose();
        }

        private void SetupMethods()
        {
            SendCustomPacket = new CompiledMethod(Sender.GetType().GetMethods()
                .First(m => m.Name == nameof(ISender.Send) && m.GetGenericArguments().Length == 2 && m.GetParameters().Length == 1));
        }

        public bool SetPacketType(Type type)
        {
            if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0))
                return false;

            var temp = type;
            while (temp.BaseType != null)
            {
                if (temp.BaseType.GUID == typeof(Packet<>).GUID)
                {
                    PacketType = type;
                    return true;
                }
                temp = temp.BaseType;
            }
            return false;
        }

        public async Task<T?> PostAsync<T, U>(U data, TimeSpan timeout) where U : ISerializable<U> where T : ISerializable<T>
        {
            var container = new RequestContainer<U>(data, RequestType.Post)
            {
                ResponseObjType = typeof(T)
            };
            await Send(container);

            return await GetResponse<T, U>(container, timeout);
        }
        private async Task<T?> GetResponse<T, U>(RequestContainer<U> container, TimeSpan timeout) where U : ISerializable<U> where T : ISerializable<T>
        {
            var resultSource = new TaskCompletionSource<IPacket>();

            if (Sender.IsAvailable && Requests.TryAdd(container.Id, resultSource))
            {
                var response = resultSource.Task;
                try
                {
                    await response.WaitAsync(timeout);
                }
                catch (TimeoutException ex)
                {
                    return default;
                }
                finally
                {
                    Requests.TryRemove(new KeyValuePair<Guid, TaskCompletionSource<IPacket>>(container.Id, resultSource));
                }

                if (response.IsCompletedSuccessfully && 
                    response.Result is Packet<RequestContainer<T>> result && result.Data != null)
                    return result.Data.Data;
                return default;
            }

            throw new Exception("Request sending error");
        }
        private void ResponseAppear(object? sender, IPacket e)
        {
            var packet = (IRequestContainer)e.DataObj;
            if (Requests.TryGetValue(packet.Id, out var request))
                request.SetResult(e);
        }

        public async Task<bool> GetAsync<U>(U data) where U : ISerializable<U>
        {
            if (!Sender.IsAvailable)
                throw new Exception("Request sending error");

            var container = new RequestContainer<U>(data, RequestType.Get);
            return await Sender.Send(container);
        }

        private async Task<bool> Send<U>(RequestContainer<U> container) where U : ISerializable<U>
        {
            if (PacketType != null)
                return await (Task<bool>)SendCustomPacket.Invoke(Sender, new Type[] { typeof(RequestContainer<U>), PacketType.MakeGenericType(typeof(RequestContainer<U>)) }, container);
            return await Sender.Send(container);
        }
    }
}
