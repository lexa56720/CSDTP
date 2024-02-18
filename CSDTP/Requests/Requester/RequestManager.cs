using CSDTP.Packets;
using CSDTP.Requests.RequestHeaders;
using System.Collections.Concurrent;
using System.Reflection;
using AutoSerializer;
using PerformanceUtils.Performance;

namespace CSDTP.Requests
{
    internal class RequestManager
    {
        public ConcurrentDictionary<Guid, TaskCompletionSource<IPacket<IRequestContainer>>> Requests = new();
        private Type? CustomPacketType { get; set; } = null;

        private CompiledMethod? CreateCustomPacket { get; set; } = null;

        public RequestManager(Type customPacketType)
        {
            if (!SetPacketType(customPacketType))
                throw new Exception("WRONG CUSTOM PACKET TYPE");

            CreateCustomPacket = new CompiledMethod(typeof(RequestManager).GetMethod(nameof(GetPacket), BindingFlags.NonPublic | BindingFlags.Instance));
        }
        public RequestManager()
        {

        }

        private bool SetPacketType(Type type)
        {
            if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0))
                return false;

            var temp = type;
            while (temp.BaseType != null)
            {
                if (temp.BaseType.GUID == typeof(Packet<>).GUID)
                {
                    CustomPacketType = type;
                    return true;
                }
                temp = temp.BaseType;
            }
            return false;
        }
        public RequestContainer<TData> PackToContainer<TResponse, TData>(TData data)
                                       where TData : ISerializable<TData>, new()
                                       where TResponse : ISerializable<TResponse>, new()
        {
            return new RequestContainer<TData>(data, RequesKind.Request)
            {
                ResponseObjType = typeof(TResponse)
            };
        }

        public RequestContainer<TData> PackToContainer<TData>(TData data)
                                     where TData : ISerializable<TData>, new()
        {
            return new RequestContainer<TData>(data, RequesKind.Data);
        }
        public IPacket PackToPacket<TData>(RequestContainer<TData> data, int replyPort)
                       where TData : ISerializable<TData>, new()
        {
            if (CustomPacketType != null)
            {
                return (IPacket)CreateCustomPacket.Invoke(this,
                        [typeof(TData), CustomPacketType.MakeGenericType(typeof(RequestContainer<TData>))],
                        data,
                        replyPort);
            }
            return GetPacket<TData, Packet<RequestContainer<TData>>>(data, replyPort);
        }
        private Packet<RequestContainer<TData>> GetPacket<TData, TPacket>(RequestContainer<TData> data, int replyPort)
                                       where TData : ISerializable<TData>, new()
                                       where TPacket : Packet<RequestContainer<TData>>, new()
        {
            return new TPacket()
            {
                Data = data,
                IsHasData = true,
                ReplyPort = replyPort,
                SendTime = DateTime.UtcNow,
            };
        }

        public bool AddRequest(IRequestContainer requestContainer)
        {
            var resultSource = new TaskCompletionSource<IPacket<IRequestContainer>>();
            return Requests.TryAdd(requestContainer.Id, resultSource);
        }
        public async Task<IPacket<IRequestContainer>?> GetResponseAsync(IRequestContainer requestContainer, TimeSpan timeout)
        {
            if (!Requests.TryGetValue(requestContainer.Id, out var response))
                return null;
            try
            {
                var result = await response.Task.WaitAsync(timeout);
                if (response.Task.IsCompletedSuccessfully && response.Task.Result is not null)
                    return result;
            }
            catch
            {
                throw;
            }
            finally
            {
                Requests.TryRemove(requestContainer.Id, out _);
            }
            return null;
        }

        public void ResponseAppear(IPacket<IRequestContainer> packet)
        {
            if (Requests.TryGetValue(packet.Data.Id, out var request))
                request.SetResult(packet);
        }
    }
}
