﻿using CSDTP.Cryptography.Providers;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using System.Net;
using AutoSerializer;
using CSDTP.Cryptography.Algorithms;

namespace CSDTP.Requests
{
    public class Requester : IDisposable
    {
        public int ReplyPort => Receiver.Port;

        private readonly ISender Sender;
        private readonly IReceiver Receiver;

        private RequestManager RequestManager = null!;
        private PacketManager PacketManager = null!;

        private bool isDisposed;

        private Requester(ISender sender, IReceiver receiver)
        {
            Sender = sender;
            Receiver = receiver;
        }

        internal static async Task<Requester> Initialize(ISender sender, IReceiver receiver, IEncryptProvider? encryptProvider = null, Type? customPacketType = null)
        {
            var requester = new Requester(sender, receiver);
            requester.PacketManager = encryptProvider == null ? new PacketManager() : new PacketManager(encryptProvider);
            if (customPacketType != null)
                requester.RequestManager = new RequestManager(customPacketType);
            else
                requester.RequestManager = new RequestManager();
            requester.Receiver.DataAppear += requester.ResponseAppear;
            await requester.Receiver.Start();
            return requester;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    Sender.Dispose();
                    Receiver.DataAppear -= ResponseAppear;
                    Receiver.Dispose();
                    PacketManager?.Dispose();
                }
                isDisposed = true;
            }
        }

        private void ResponseAppear(object? sender, (IPAddress from, byte[] data) e)
        {
            if (e.data.Length == 0)
                return;

            var decryptedData = PacketManager.DecryptBytes(e.data);
            if (decryptedData.Length == 0)
                return;

            var packet = PacketManager.GetPacketFromBytes(decryptedData);
            if (packet == null)
                return;

            packet.ReceiveTime = DateTime.UtcNow;
            packet.Source = e.from;

            RequestManager.ResponseAppear(packet);
        }

        public async Task<bool> SendAsync<TData>(TData data)
                                where TData : ISerializable<TData>, new()
        {
            var container = RequestManager.PackToContainer(data);
            container.RequestKind = RequesKind.Data;
            var packet = RequestManager.PackToPacket(container, -1);
            var encrypter = PacketManager.GetEncrypter(packet);
            var packetBytes = PacketManager.GetBytes(packet);

            var cryptedPacketBytes = PacketManager.EncryptBytes(packetBytes.bytes, packetBytes.posToCrypt, encrypter);

            return await Sender.SendBytes(cryptedPacketBytes);
        }
        public async Task<TResponse?> RequestAsync<TResponse, TRequest>(TRequest data, TimeSpan timeout, CancellationToken token)
                                      where TRequest : ISerializable<TRequest>, new()
                                      where TResponse : ISerializable<TResponse>, new()
        {
            try
            {
                var container = RequestManager.PackToContainer<TResponse, TRequest>(data);
                container.RequestKind = RequesKind.Request;
                container.ResponseObjType = typeof(TResponse);
                var packet = RequestManager.PackToPacket(container, ReplyPort);
                var encrypter = PacketManager.GetEncrypter(packet);
                var packetBytes = PacketManager.GetBytes(packet);

                var cryptedPacketBytes = PacketManager.EncryptBytes(packetBytes.bytes, packetBytes.posToCrypt, encrypter);

                if (!RequestManager.AddRequest(container))
                    return default;

                await Sender.SendBytes(cryptedPacketBytes);
                var responsePacket = await RequestManager.GetResponseAsync(container, timeout, token);

                if (responsePacket == null)
                    return default;

                return (TResponse)responsePacket.Data.DataObj;
            }
            catch
            {
                return default;
            }
        }

        public async Task<TResponse?> RequestAsync<TResponse, TRequest>(TRequest data, TimeSpan timeout)
                  where TRequest : ISerializable<TRequest>, new()
                  where TResponse : ISerializable<TResponse>, new()
        {
            return await RequestAsync<TResponse, TRequest>(data, timeout, default);
        }
    }
}
