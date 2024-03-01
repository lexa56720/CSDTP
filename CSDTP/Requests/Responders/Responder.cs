using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using System.Net;
using AutoSerializer;
using PerformanceUtils.Performance;
using PerformanceUtils.Collections;

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

        internal Responder(IReceiver receiver, IEncryptProvider? encryptProvider = null, Type? customPacketType = null)
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

        public async Task Start()
        {
            if (IsRunning)
                return;

            await Receiver.Start();
            RequestsQueue.Start();
            IsRunning = true;
        }

        public async Task Stop()
        {
            if (!IsRunning)
                return;

            await Receiver.Stop();
            RequestsQueue.Stop();
            IsRunning = false;
        }

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
            await Reply(requestPacket, response);
        }
        public async Task<bool> ResponseManually<T>(IPacket<IRequestContainer> requestPacket, T responseObj) where T : ISerializable<T>, new()
        {
            var bytes = GetResponseBytes(requestPacket, responseObj);
            return await Reply(requestPacket, bytes);
        }
        private async Task<bool> Reply(IPacket<IRequestContainer>? requestPacket, byte[]? response)
        {
            if (requestPacket == null)
                return false;
            if (requestPacket.Data.RequestKind == RequesKind.Data)
                return true;

            var sender = GetSender(new IPEndPoint(requestPacket.Source, requestPacket.ReplyPort));

            if (response != null)
                return await sender.SendBytes(response);
            return false;
        }
        protected abstract ISender GetSender(IPEndPoint endPoint);

        protected (byte[]? response, IPacket<IRequestContainer>? request) HandleRequest(IPAddress from, byte[] data)
        {
            var decryptedData = PacketManager.DecryptBytes(data);
            if (decryptedData.Length == 0)
                return (null, null);

            var requestPacket = PacketManager.GetPacketFromBytes(decryptedData);
            if (requestPacket == null)
                return (null, null);

            requestPacket.ReceiveTime = DateTime.UtcNow;
            requestPacket.Source = from;


            if (requestPacket.Data.RequestKind == RequesKind.Request && requestPacket.Data.ResponseObjType != null)
                return (GetResponse(requestPacket), requestPacket);

            if (requestPacket.Data.RequestKind == RequesKind.Data && DataHandlers.TryGetValue(requestPacket.Data.DataType, out var handler))
                handler(requestPacket.Data.DataObj, requestPacket);

            return (null, requestPacket);
        }
        private byte[]? GetResponse(IPacket<IRequestContainer> requestPacket)
        {
            if (!RequestHandlers.TryGetValue((requestPacket.Data.DataType, requestPacket.Data.ResponseObjType), out var handler))
                return null;

            var responseData = handler(requestPacket.Data.DataObj, requestPacket);
            if (responseData == null)
                return null;

            return GetResponseBytes(requestPacket, responseData);
        }

        private byte[]? GetResponseBytes(IPacket<IRequestContainer> requestPacket, object responseObj)
        {
            var responseContainerType = typeof(RequestContainer<>).MakeGenericType(requestPacket.Data.ResponseObjType);
            var responseContainer = (IRequestContainer)CompiledActivator.CreateInstance(responseContainerType);

            responseContainer.Id = requestPacket.Data.Id;
            responseContainer.RequestKind = RequesKind.Response;
            responseContainer.DataType = requestPacket.Data.ResponseObjType;
            responseContainer.DataObj = responseObj;

            var responsePacket = (IPacket)PackToPacket.Invoke(RequestManager, requestPacket.Data.ResponseObjType, responseContainer, 0);
            var responseBytes = PacketManager.GetBytes(responsePacket);
            var cryptedBytes = PacketManager.EncryptBytes(responseBytes.bytes, responseBytes.posToCrypt, responsePacket, requestPacket);

            return cryptedBytes;
        }
    }
}
