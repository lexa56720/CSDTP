﻿using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using CSDTP.Utils.Performance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CSDTP.Requests
{
    internal class RequestManager
    {
        public ConcurrentDictionary<Guid, TaskCompletionSource<IPacket>> Requests = new();
        private Type? CustomPacketType { get; set; } = null;

        private CompiledMethod? CreateCustomPacket { get; set; } = null;

        public RequestManager(Type customPacketType)
        {
            CustomPacketType = customPacketType;
            CreateCustomPacket = new CompiledMethod(GetType()
                .GetMethods()
                .First(m => m.Name == nameof(GetPacket) &&
                            m.GetGenericArguments().Length == 2 &&
                            m.GetParameters().Length == 2));
        }
        public RequestManager()
        {

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
            return new RequestContainer<TData>(data, RequestType.Post)
            {
                ResponseObjType = typeof(TResponse)
            };
        }

        public RequestContainer<TData> PackToContainer<TData>(TData data)
                                     where TData : ISerializable<TData>, new()
        {
            return new RequestContainer<TData>(data, RequestType.Get);
        }
        public IPacket PackToPacket<TData>(RequestContainer<TData> data, int replyPort)
                       where TData : ISerializable<TData>, new()
        {
            if (CustomPacketType != null)
            {
                return (IPacket)CreateCustomPacket.Invoke(this, typeof(TData), CustomPacketType, data, replyPort);
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
                SendTime = DateTime.Now,
            };
        }

        public bool AddRequest(IRequestContainer requestContainer)
        {
            var resultSource = new TaskCompletionSource<IPacket>();
            return Requests.TryAdd(requestContainer.Id, resultSource);
        }
        public async Task<IPacket?> GetResponseAsync(IRequestContainer requestContainer, TimeSpan timeout)
        {
            if (!Requests.TryGetValue(requestContainer.Id, out var response))
                return null;
            try
            {
                var result = await response.Task.WaitAsync(timeout);
                if (response.Task.IsCompletedSuccessfully && response.Task.Result is not null)
                    return result;
            }
            finally
            {
                Requests.TryRemove(requestContainer.Id, out _);
            }
            return null;
        }

        public void ResponseAppear(IRequestContainer requestContainer, IPacket packet)
        {
            if (Requests.TryGetValue(requestContainer.Id, out var request))
                request.SetResult(packet);
        }
    }
}