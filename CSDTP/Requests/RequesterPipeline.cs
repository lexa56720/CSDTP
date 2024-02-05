using CSDTP.Cryptography.Algorithms;
using CSDTP.Cryptography.Providers;
using CSDTP.DosProtect;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using CSDTP.Utils;
using CSDTP.Utils.Performance;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    public class RequesterPipeline : IDisposable
    {
        public int ReplyPort => Receiver.Port;

        private ISender Sender;
        private IReceiver Receiver;

        private RequestManager RequestManager = null!;
        private PacketManager PacketManager = null!;

        private bool isDisposed;

        public RequesterPipeline(ISender sender, IReceiver receiver, IEncryptProvider? encryptProvider = null, Type? customPacketType = null)
        {
            Sender = sender;
            Receiver = receiver;
            Initialize(customPacketType, encryptProvider);
        }
        public RequesterPipeline(IPEndPoint destination, int replyPort, Protocol protocol, IEncryptProvider? encryptProvider = null, Type? customPacketType = null)
        {
            Sender = SenderFactory.CreateSender(destination, protocol);
            Receiver = ReceiverFactory.CreateReceiver(replyPort, protocol);
            Initialize(customPacketType, encryptProvider);
        }
        private void Initialize(Type? customPacketType, IEncryptProvider? encryptProvider)
        {
            PacketManager = encryptProvider == null ? new PacketManager() : new PacketManager(encryptProvider);
            Receiver.DataAppear += ResponseAppear;
            Receiver.Start();
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
            var decryptedData = PacketManager.DecryptBytes(e.data);
            var packet = PacketManager.GetResponsePacket(decryptedData);
            packet.ReceiveTime = DateTime.Now;
            packet.Source = e.from;

            if (packet.DataObj is IRequestContainer container)
                RequestManager.ResponseAppear(container, packet);
        }

        public async Task<bool> SendAsync<TData>(TData data)
                                where TData : ISerializable<TData>, new()
        {
            var container = RequestManager.PackToContainer(data);
            container.RequestKind = RequesKind.Data;
            var packet = RequestManager.PackToPacket(container, -1);

            var packetBytes = PacketManager.GetBytes(packet);

            var cryptedPacketBytes = PacketManager.EncryptBytes(packet, packetBytes.bytes, packetBytes.posToCrypt);

            return await Sender.SendBytes(cryptedPacketBytes);
        }
        public async Task<TResponse?> SendRequestAsync<TResponse, TRequest>(TRequest data, TimeSpan timeout)
                                      where TRequest : ISerializable<TRequest>, new()
                                      where TResponse : ISerializable<TResponse>, new()
        {
            var container = RequestManager.PackToContainer<TResponse, TRequest>(data);
            container.RequestKind = RequesKind.Request;
            container.ResponseObjType = typeof(TResponse);
            var packet = RequestManager.PackToPacket(container, ReplyPort);

            var packetBytes = PacketManager.GetBytes(packet);

            var cryptedPacketBytes = PacketManager.EncryptBytes(packet, packetBytes.bytes, packetBytes.posToCrypt);

            if (!RequestManager.AddRequest(container))
                return default;

            await Sender.SendBytes(cryptedPacketBytes);
            var responsePacket = await RequestManager.GetResponseAsync(container, timeout);

            if (responsePacket == null)
                return default;

            return (TResponse)((IRequestContainer)responsePacket.DataObj).DataObj;
        }
    }
}
