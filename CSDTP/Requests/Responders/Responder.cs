using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Requests.RequestHeaders;
using System.Net;
using AutoSerializer;
using PerformanceUtils.Performance;
using PerformanceUtils.Collections;
using System.Net.Sockets;
using CSDTP.Protocols.Communicators;

namespace CSDTP.Requests
{
    public class Responder : IDisposable
    {
        public required Protocol Protocol { get; init; }
        public int ListenPort => Communicator.ListenPort;
        public bool IsRunning { get; private set; }
        public bool ResponseIfNull { get; set; } = false;

        private readonly ICommunicator Communicator;

        private readonly PacketManager PacketManager;
        private readonly RequestManager RequestManager;

        private readonly Dictionary<Type, Func<object, IPacketInfo, Func<byte[], Task<bool>>, Task>> DataHandlers = new();
        private readonly Dictionary<(Type, Type), Func<object, IPacketInfo, Func<byte[], Task<bool>>, Task<object?>>> RequestHandlers = new();

        private readonly CompiledMethod PackToPacket = new(typeof(RequestManager).GetMethods().Single(m=>m.Name== nameof(RequestManager.PackToPacket) && m.GetGenericArguments().Length==1));
        private QueueProcessor<DataInfo> RequestsQueue { get; set; }

        private bool IsDisposed;
        internal Responder(ICommunicator communicator, IEncryptProvider? encryptProvider = null, Type? customPacketType = null)
        {
            Communicator = communicator;
            RequestsQueue = new QueueProcessor<DataInfo>(DataAppear, 32, TimeSpan.FromMilliseconds(10));
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

        public bool RegisterDataHandler<TData>(Func<TData, IPacketInfo, Task> action)
        {
            return DataHandlers.TryAdd(typeof(TData),
                                       new Func<object, IPacketInfo, Func<byte[], Task<bool>>, Task>(async (o, i, f) => await action((TData)o, i)));
        }
        public bool RegisterDataHandler<TData>(Func<TData, Task> action)
        {
            return DataHandlers.TryAdd(typeof(TData),
                                       new Func<object, IPacketInfo, Func<byte[], Task<bool>>, Task>(async (o, i, f) => await action((TData)o)));
        }
        public bool RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, Task<TResponse?>> action)
                    where TResponse : ISerializable<TResponse>, new()
        {
            return RequestHandlers.TryAdd((typeof(TRequest),
                                          typeof(TResponse)),
                                          new Func<object, IPacketInfo, Func<byte[], Task<bool>>, Task<object?>>(async (o, i, f) => await action((TRequest)o)));
        }
        public bool RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, IPacketInfo, Task<TResponse?>> action)
            where TResponse : ISerializable<TResponse>, new()
        {
            return RequestHandlers.TryAdd((typeof(TRequest),
                                          typeof(TResponse)),
                                          new Func<object, IPacketInfo, Func<byte[], Task<bool>>, Task<object?>>(async (o, i, f) => await action((TRequest)o, i)));
        }
        public bool RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, IPacketInfo, Func<byte[], Task<bool>>, Task<TResponse?>> action)
                    where TResponse : ISerializable<TResponse>, new()
        {
            return RequestHandlers.TryAdd((typeof(TRequest),
                                          typeof(TResponse)),
                                          new Func<object, IPacketInfo, Func<byte[], Task<bool>>, Task<object?>>(async (o, i, f) => await action((TRequest)o, i, f)));
        }


        private async Task DataAppear(DataInfo dataInfo)
        {
            var (response, requestPacket) = await HandleRequest(dataInfo);
            await Reply(requestPacket, response, dataInfo.ReplyFunc);
        }
        public async Task<bool> ResponseManually<T>(IPacket<IRequestContainer> requestPacket, T responseObj, Func<byte[], Task<bool>> reply)
            where T : ISerializable<T>, new()
        {
            var bytes = await GetResponseBytes(requestPacket, responseObj);
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

            if (response == null && ResponseIfNull)
            {
                await replyFunc([]);
                return false;
            }
            return false;
        }

        private async Task<(byte[]? response, IPacket<IRequestContainer>? request)> HandleRequest(DataInfo dataInfo)
        {
            //Расшифровка байтов
            var decryptedData = await PacketManager.DecryptBytes(dataInfo.Data);
            if (decryptedData.Length == 0)
                return (null, null);

            //Десериализация пакета
            var requestPacket = PacketManager.GetPacketFromBytes(decryptedData);
            if (requestPacket == null)
                return (null, null);

            //Установка информации о пакете
            requestPacket.ReceiveTime = DateTime.UtcNow;
            requestPacket.Source = dataInfo.From;

            //Получение объекта ответа, если запрос предполагает ответ
            if (requestPacket.Data.RequestKind == RequesKind.Request && requestPacket.Data.ResponseObjType != null)
                return (await GetResponse(requestPacket, dataInfo.ReplyFunc), requestPacket);

            //Передача запроса методу, если ответ не предполагается
            if (requestPacket.Data.RequestKind == RequesKind.Data && DataHandlers.TryGetValue(requestPacket.Data.DataType, out var handler))
                await handler(requestPacket.Data.DataObj, requestPacket, dataInfo.ReplyFunc);

            return (null, requestPacket);
        }
        private async Task<byte[]?> GetResponse(IPacket<IRequestContainer> requestPacket, Func<byte[], Task<bool>> replyFunc)
        {
            if (!RequestHandlers.TryGetValue((requestPacket.Data.DataType, requestPacket.Data.ResponseObjType), out var handler))
                return null;

            var responseData = await handler(requestPacket.Data.DataObj, requestPacket, replyFunc);
            if (responseData == null)
                return null;

            return await GetResponseBytes(requestPacket, responseData);
        }

        private async Task<byte[]?> GetResponseBytes(IPacket<IRequestContainer> requestPacket, object responseObj)
        {
            var responseContainerType = typeof(RequestContainer<>).MakeGenericType(requestPacket.Data.ResponseObjType);
            var responseContainer = (IRequestContainer)CompiledActivator.CreateInstance(responseContainerType);

            responseContainer.Id = requestPacket.Data.Id;
            responseContainer.RequestKind = RequesKind.Response;
            responseContainer.DataType = requestPacket.Data.ResponseObjType;
            responseContainer.DataObj = responseObj;

            var responsePacket = (IPacket)PackToPacket.Invoke(RequestManager, requestPacket.Data.ResponseObjType, responseContainer, 0);
            var encrypter = await PacketManager.GetEncrypter(responsePacket, requestPacket);
            var responseBytes = PacketManager.GetBytes(responsePacket);

            var cryptedBytes = PacketManager.EncryptBytes(responseBytes.bytes, responseBytes.posToCrypt, encrypter);

            return cryptedBytes;
        }
    }
}
