using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using System.Net;
using AutoSerializer;
using PerformanceUtils.Performance;
using PerformanceUtils.Collections;
using System.Net.Sockets;

namespace CSDTP.Requests
{
    public class Responder : IDisposable
    {
        public required Protocol Protocol { get; init; }

        private readonly TimeSpan SenderLifeTime = TimeSpan.FromSeconds(60);
        public int ListenPort => Communicator.ListenPort;
        public bool IsRunning { get; private set; }

        private readonly ICommunicator Communicator;

        private readonly PacketManager PacketManager;
        private readonly RequestManager RequestManager;

        private readonly Dictionary<Type, Action<object, IPacketInfo, Func<byte[], Task<bool>>>> DataHandlers = new();
        private readonly Dictionary<(Type, Type), Func<object, IPacketInfo, Func<byte[], Task<bool>>, object?>> RequestHandlers = new();

        private readonly CompiledMethod PackToPacket = new(typeof(RequestManager).GetMethod(nameof(RequestManager.PackToPacket)));
        private QueueProcessor<(IPAddress, byte[], Func<byte[], Task<bool>>)> RequestsQueue { get; set; }

        private bool IsDisposed;
        internal Responder(ICommunicator communicator, IEncryptProvider? encryptProvider = null, Type? customPacketType = null)
        {
            Communicator = communicator;
            RequestsQueue = new QueueProcessor<(IPAddress, byte[], Func<byte[], Task<bool>>)>(DataAppear, 32, TimeSpan.FromMilliseconds(10));
            PacketManager = encryptProvider == null ? new PacketManager() : new PacketManager(encryptProvider);
            if (customPacketType != null)
                RequestManager = new RequestManager(customPacketType);
            else
                RequestManager = new RequestManager();
            Communicator.DataAppear += (o, e) => RequestsQueue.Add(e);
        }


        public async void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            await Stop();
            Communicator.Dispose();
        }

        public async Task Start()
        {
            if (IsRunning)
                return;

            await Communicator.Start();
            RequestsQueue.Start();
            IsRunning = true;
        }

        public async Task Stop()
        {
            if (!IsRunning)
                return;

            await Communicator.Stop();
            RequestsQueue.Stop();
            IsRunning = false;
        }

        public bool RegisterDataHandler<TData>(Action<TData, IPacketInfo> action)
        {
            return DataHandlers.TryAdd(typeof(TData),
                                       new Action<object,
                                       IPacketInfo, Func<byte[], Task<bool>>>((o, i, f) => action((TData)o, i)));
        }
        public bool RegisterDataHandler<TData>(Action<TData> action)
        {
            return DataHandlers.TryAdd(typeof(TData),
                                       new Action<object, IPacketInfo, Func<byte[], Task<bool>>>((o, i, f) => action((TData)o)));
        }
        public bool RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, TResponse?> action)
                    where TResponse : ISerializable<TResponse>, new()
        {
            return RequestHandlers.TryAdd((typeof(TRequest),
                                          typeof(TResponse)),
                                          new Func<object, IPacketInfo, Func<byte[], Task<bool>>, object?>((o, i, f) => action((TRequest)o)));
        }
        public bool RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, IPacketInfo, TResponse?> action)
            where TResponse : ISerializable<TResponse>, new()
        {
            return RequestHandlers.TryAdd((typeof(TRequest),
                                          typeof(TResponse)),
                                          new Func<object, IPacketInfo, Func<byte[], Task<bool>>, object?>((o, i, f) => action((TRequest)o, i)));
        }
        public bool RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, IPacketInfo, Func<byte[], Task<bool>>, TResponse?> action)
                    where TResponse : ISerializable<TResponse>, new()
        {
            return RequestHandlers.TryAdd((typeof(TRequest),
                                          typeof(TResponse)),
                                          new Func<object, IPacketInfo, Func<byte[], Task<bool>>, object?>((o, i, f) => action((TRequest)o, i, f)));
        }


        private async Task DataAppear((IPAddress from, byte[] data, Func<byte[], Task<bool>> replyFunc) request)
        {
            var (response, requestPacket) = HandleRequest(request.from, request.data, request.replyFunc);
            await Reply(requestPacket, response, request.replyFunc);
        }
        public async Task<bool> ResponseManually<T>(IPacket<IRequestContainer> requestPacket, T responseObj, Func<byte[], Task<bool>> reply)
            where T : ISerializable<T>, new()
        {
            var bytes = GetResponseBytes(requestPacket, responseObj);
            return await Reply(requestPacket, bytes, reply);
        }
        private async Task<bool> Reply(IPacket<IRequestContainer>? requestPacket, byte[]? response, Func<byte[], Task<bool>> replyFunc)
        {
            if (requestPacket == null)
                return false;
            if (requestPacket.Data.RequestKind == RequesKind.Data)
                return true;

            if (response != null)
                return await replyFunc(response);
            else
            {
                await replyFunc([]);
                return false;
            }
        }

        protected (byte[]? response, IPacket<IRequestContainer>? request) HandleRequest(IPAddress from, byte[] data, Func<byte[], Task<bool>> replyFunc)
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
                return (GetResponse(requestPacket, replyFunc), requestPacket);

            if (requestPacket.Data.RequestKind == RequesKind.Data && DataHandlers.TryGetValue(requestPacket.Data.DataType, out var handler))
                handler(requestPacket.Data.DataObj, requestPacket, replyFunc);

            return (null, requestPacket);
        }
        private byte[]? GetResponse(IPacket<IRequestContainer> requestPacket, Func<byte[], Task<bool>> replyFunc)
        {
            if (!RequestHandlers.TryGetValue((requestPacket.Data.DataType, requestPacket.Data.ResponseObjType), out var handler))
                return null;

            var responseData = handler(requestPacket.Data.DataObj, requestPacket, replyFunc);
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
            var encrypter = PacketManager.GetEncrypter(responsePacket, requestPacket);
            var responseBytes = PacketManager.GetBytes(responsePacket);

            var cryptedBytes = PacketManager.EncryptBytes(responseBytes.bytes, responseBytes.posToCrypt, encrypter);

            return cryptedBytes;
        }
    }
}
