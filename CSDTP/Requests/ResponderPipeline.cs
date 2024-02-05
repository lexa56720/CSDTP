using CSDTP.Cryptography.Providers;
using CSDTP.DosProtect;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using CSDTP.Utils.Performance;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    public abstract class ResponderPipeline:IDisposable
    {
        public ITrafficLimiter? TrafficLimiter { get; init; }
        public bool ResponseIfNull { get; set; }
        public abstract int Port { get; }

        public bool IsRunning { get; private set; }

        private readonly PacketManager PacketManager;
        private readonly RequestManager RequestManager;

        private readonly Dictionary<Type, Action<object, IPacketInfo>> DataHandlers = new();
        private readonly Dictionary<(Type, Type), Func<object, IPacketInfo, object?>> RequestHandlers = new();

        private readonly CompiledMethod PackToPacket = new(typeof(RequestManager).GetMethod(nameof(RequestManager.PackToPacket)));


        protected bool isDisposed;
        public ResponderPipeline(IEncryptProvider? encryptProvider = null, Type? customPacketType = null)
        {
            PacketManager = encryptProvider == null ? new PacketManager() : new PacketManager(encryptProvider);
            if (customPacketType != null)
                RequestManager = new RequestManager(customPacketType);
            else
                RequestManager = new RequestManager();
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected abstract void Dispose(bool disposing);


        public void Start()
        {
            if(!IsRunning)
            {
                Start(IsRunning);
                IsRunning = true;
            }
        }
        protected abstract void Start(bool isRunning);

        public void Stop()
        {
            if (IsRunning)
            {
                Stop(IsRunning);
                IsRunning = false;
            }
        }
        protected abstract void Stop(bool isRunning);

        public bool RegisterDataHandler<TData>(Action<TData, IPacketInfo> action)
        {
            return DataHandlers.TryAdd(typeof(TData),
                                       new Action<object,
                                       IPacketInfo>((o, i) =>
                                         action((TData)o, i))
                                       );
        }
        public bool RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, IPacketInfo, TResponse?> action)
                    where TResponse : ISerializable<TResponse>, new()
        {
            return RequestHandlers.TryAdd((typeof(TRequest),
                                          typeof(TResponse)),
                                          new Func<object, IPacketInfo, object?>((o, i) =>
                                            action((TRequest)o, i))
                                          );
        }
        public bool RegisterDataHandler<TData>(Action<TData> action)
        {
            return DataHandlers.TryAdd(typeof(TData), new Action<object, IPacketInfo>((o, i) => action((TData)o)));
        }
        public bool RegisterRequestHandler<TRequest, TResponse>(Func<TRequest, TResponse?> action)
                    where TResponse : ISerializable<TResponse>, new()
        {
            return RequestHandlers.TryAdd((typeof(TRequest),
                                          typeof(TResponse)),
                                          new Func<object, IPacketInfo, object?>((o, i) =>
                                            action((TRequest)o))
                                          );
        }

        protected (byte[]? response, IPacket request) HandleRequest(IPAddress from, byte[] data)
        {
            var decryptedData = PacketManager.DecryptBytes(data);
            var packet = PacketManager.GetResponsePacket(decryptedData);
            packet.ReceiveTime = DateTime.Now;
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
            var responseContainerType = typeof(RequestContainer<>).MakeGenericType(container.ResponseObjType);
            var responseContainer = (IRequestContainer)Activator.CreateInstance(responseContainerType);

            responseContainer.Id = container.Id;
            responseContainer.RequestKind = RequesKind.Response;
            responseContainer.DataType = container.ResponseObjType;
            responseContainer.DataObj = responseData;

            var responsePacket = (IPacket)PackToPacket.Invoke(RequestManager, container.ResponseObjType, responseContainer, 0);
            var responseBytes = PacketManager.GetBytes(responsePacket);
            var cryptedBytes = PacketManager.EncryptBytes(responsePacket, responseBytes.bytes, responseBytes.posToCrypt);

            return cryptedBytes;
        }
    }
}
