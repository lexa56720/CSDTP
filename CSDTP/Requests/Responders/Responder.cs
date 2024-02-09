using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using CSDTP.Utils.Collections;
using CSDTP.Utils.Performance;
using System.Net;
using AutoSerializer;

namespace CSDTP.Requests
{
    public abstract class Responder : IDisposable
    {
        public abstract Protocol Protocol { get; }
        public int ListenPort => Receiver.Port;
        public bool IsRunning { get; private set; }

        private readonly IReceiver Receiver;

        private readonly PacketManager PacketManager;
        private readonly RequestManager RequestManager;

        private readonly Dictionary<Type, Action<object, IPacketInfo>> DataHandlers = new();
        private readonly Dictionary<(Type, Type), Func<object, IPacketInfo, object?>> RequestHandlers = new();

        private readonly CompiledMethod PackToPacket = new(typeof(RequestManager).GetMethod(nameof(RequestManager.PackToPacket)));
        private QueueProcessorAsync<(IPAddress, byte[])> RequestsQueue { get; set; }

        protected bool isDisposed;

        public Responder(IReceiver receiver, IEncryptProvider? encryptProvider = null, Type? customPacketType = null)
        {
            Receiver = receiver;
            RequestsQueue = new QueueProcessorAsync<(IPAddress, byte[])>(DataAppear, 32, TimeSpan.FromMilliseconds(10));
            PacketManager = encryptProvider == null ? new PacketManager() : new PacketManager(encryptProvider);
            if (customPacketType != null)
                RequestManager = new RequestManager(customPacketType);
            else
                RequestManager = new RequestManager();
            Receiver.DataAppear += (o, e) => RequestsQueue.Add(e);
        }
        public void Dispose()
        {
            if (!isDisposed)
            {
                Dispose(disposing: true);
                isDisposed = true;
            }
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            Stop();
            Receiver.Dispose();
        }

        public void Start()
        {
            if (IsRunning)
                return;

            Receiver.Start();
            RequestsQueue.Start();
            IsRunning = true;
        }
        protected virtual void Start(bool isRunning) { }

        public void Stop()
        {
            if (!IsRunning)
                return;

            Receiver.Stop();
            RequestsQueue.Stop();
            IsRunning = false;
        }
        protected virtual void Stop(bool isRunning) { }

        public bool RegisterDataHandler<TData>(Action<TData, IPacketInfo> action)
        {
            return DataHandlers.TryAdd(typeof(TData),
                                       new Action<object,
                                       IPacketInfo>((o, i) => action((TData)o, i)));
        }
        public bool RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, IPacketInfo, TResponse?> action)
                    where TResponse : ISerializable<TResponse>, new()
        {
            return RequestHandlers.TryAdd((typeof(TRequest),
                                          typeof(TResponse)),
                                          new Func<object, IPacketInfo, object?>((o, i) => action((TRequest)o, i)));
        }
        public bool RegisterDataHandler<TData>(Action<TData> action)
        {
            return DataHandlers.TryAdd(typeof(TData),
                                       new Action<object, IPacketInfo>((o, i) => action((TData)o)));
        }
        public bool RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, TResponse?> action)
                    where TResponse : ISerializable<TResponse>, new()
        {
            return RequestHandlers.TryAdd((typeof(TRequest),
                                          typeof(TResponse)),
                                          new Func<object, IPacketInfo, object?>((o, i) => action((TRequest)o)));
        }

        private async Task DataAppear((IPAddress from, byte[] data) request)
        {
            var (response, requestPacket) = HandleRequest(request.from, request.data);
            await Reply((IRequestContainer)requestPacket.DataObj, requestPacket, response);
        }
        public async Task<bool> ResponseManually<T>(IPacket requestPacket, T responseObj) where T : ISerializable<T>, new()
        {
            if (requestPacket.DataObj is not IRequestContainer container)
                return false;
            var bytes = GetResponseBytes(container, responseObj);
            return await Reply(container, requestPacket, bytes);
        }
        private async Task<bool> Reply(IRequestContainer requestContainer, IPacket requestPacket, byte[]? response)
        {
            if (requestContainer.RequestKind == RequesKind.Data)
                return true;
            var sender = GetSender(new IPEndPoint(requestPacket.Source, requestPacket.ReplyPort));

            if (response != null)
                return await sender.SendBytes(response);
            return false;
        }
        protected abstract ISender GetSender(IPEndPoint endPoint);

        protected (byte[]? response, IPacket request) HandleRequest(IPAddress from, byte[] data)
        {
            var decryptedData = PacketManager.DecryptBytes(data);
            var packet = PacketManager.GetResponsePacket(decryptedData);
            packet.ReceiveTime = DateTime.UtcNow;
            packet.Source = from;

            var container = (IRequestContainer)packet.DataObj;

            if (container == null)
                return (null, packet);

            if (container.RequestKind == RequesKind.Request)
                return (GetResponse(container, packet), packet);

            if (DataHandlers.TryGetValue(container.DataType, out var handler))
                handler(container.DataObj, packet);

            return (null, packet);
        }
        private byte[]? GetResponse(IRequestContainer container, IPacket packet)
        {
            if (!RequestHandlers.TryGetValue((container.DataType, container.ResponseObjType), out var handler))
                return null;

            var responseData = handler(container.DataObj, packet);
            if (responseData == null)
                return null;

            return GetResponseBytes(container, responseData);
        }

        private byte[] GetResponseBytes(IRequestContainer container, object responseObj)
        {
            var responseContainerType = typeof(RequestContainer<>).MakeGenericType(container.ResponseObjType);
            var responseContainer = (IRequestContainer)CompiledActivator.CreateInstance(responseContainerType);

            responseContainer.Id = container.Id;
            responseContainer.RequestKind = RequesKind.Response;
            responseContainer.DataType = container.ResponseObjType;
            responseContainer.DataObj = responseObj;

            var responsePacket = (IPacket)PackToPacket.Invoke(RequestManager, container.ResponseObjType, responseContainer, 0);
            var responseBytes = PacketManager.GetBytes(responsePacket);
            var cryptedBytes = PacketManager.EncryptBytes(responsePacket, responseBytes.bytes, responseBytes.posToCrypt);

            return cryptedBytes;
        }
    }
}
