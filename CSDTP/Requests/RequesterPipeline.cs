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
    internal class RequesterPipeline : IDisposable
    {
        public ISender Sender { get; init; }
        public IReceiver Receiver { get; init; }
        public IEncryptProvider? EncryptProvider { get; init; }

        public int ReplyPort => Receiver.Port;


        private RequestManager RequestManager = null!;

        private CompiledActivator PacketActivator = new CompiledActivator();


        private bool isDisposed;

        public RequesterPipeline(ISender sender, IReceiver receiver, Type? customPacketType = null)
        {
            Sender = sender;
            Receiver = receiver;
            Initialize(customPacketType);
        }
        public RequesterPipeline(IPEndPoint destination, int replyPort, Protocol protocol, Type? customPacketType = null)
        {
            Sender = SenderFactory.CreateSender(destination, replyPort, protocol);
            Receiver = ReceiverFactory.CreateReceiver(replyPort, protocol);
            Initialize(customPacketType);
        }
        private void Initialize(Type? customPacketType)
        {
            Receiver.DataAppear += ResponseAppear;
            Receiver.Start();
            if (customPacketType != null)
                RequestManager = new RequestManager(customPacketType);
            else
                RequestManager = new RequestManager();
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
                    EncryptProvider?.Dispose();
                }
                isDisposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        private void ResponseAppear(object? sender, (IPAddress from, byte[] data) e)
        {
            var decryptedData = DecryptBytes(e.data);
            var packet = GetResponsePacket(decryptedData);
            if (packet.DataObj is IRequestContainer container)
                RequestManager.ResponseAppear(container, packet);
        }

        public async Task<bool> SendAsync<TData>(TData data) where TData : ISerializable<TData>, new()
        {
            var container = RequestManager.PackToContainer(data);
            var packet = RequestManager.PackToPacket(container, -1);

            var packetBytes = GetBytes(packet);

            var cryptedPacketBytes = EncryptBytes(packet, packetBytes.bytes, packetBytes.posToCrypt);

            return await Sender.Send(cryptedPacketBytes);
        }
        public async Task<TResponse?> SendRequestAsync<TResponse, TRequest>(TRequest data, TimeSpan timeout)
                                      where TRequest : ISerializable<TRequest>, new()
                                      where TResponse : ISerializable<TResponse>, new()
        {
            var container = RequestManager.PackToContainer<TResponse, TRequest>(data);
            var packet = RequestManager.PackToPacket(container, ReplyPort);

            var packetBytes = GetBytes(packet);

            var cryptedPacketBytes = EncryptBytes(packet, packetBytes.bytes, packetBytes.posToCrypt);

            if (!RequestManager.AddRequest(container))
                return default;

            await Sender.Send(cryptedPacketBytes);
            var responsePacket = await RequestManager.GetResponseAsync(container, timeout);

            if (responsePacket == null)
                  return default;

            return (TResponse)((IRequestContainer)responsePacket.DataObj).DataObj;   
        }

        private (byte[] bytes, int posToCrypt) GetBytes(IPacket packet)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.WriteBytes(Compressor.Compress(packet.TypeOfPacket.AssemblyQualifiedName));
            packet.SerializePacket(bw);
            packet.SerializeProtectedCustomData(bw);

            var posToCrypt = (int)ms.Position;
            packet.SerializeUnprotectedCustomData(bw);

            var bytes = ms.ToArray();
            return (bytes, posToCrypt);
        }
        private byte[] EncryptBytes(IPacketInfo packetInfo, byte[] bytes, int cryptedPos)
        {
            if (EncryptProvider == null)
                return bytes;

            var encrypter = EncryptProvider.GetEncrypter(packetInfo);
            if (encrypter == null)
                return bytes;

            var crypted = encrypter.Crypt(bytes, 0, cryptedPos);

            var result = new byte[sizeof(int) + cryptedPos + crypted.Length];
            Array.Copy(BitConverter.GetBytes(crypted.Length), result, sizeof(int));
            Array.Copy(crypted, 0, result, sizeof(int), crypted.Length);
            Array.Copy(bytes, cryptedPos, result, sizeof(int) + crypted.Length, bytes.Length - cryptedPos);

            EncryptProvider.DisposeEncrypter(encrypter);
            return result;
        }

        private byte[] DecryptBytes(byte[] bytes)
        {
            if (EncryptProvider == null)
                return bytes;

            var cryptedLength = BitConverter.ToInt32(bytes, 0);

            var decrypter = EncryptProvider.GetDecrypter(new ReadOnlySpan<byte>(bytes, cryptedLength, bytes.Length - cryptedLength));
            if (decrypter == null)
                return bytes;

            var decryptedData = decrypter.Decrypt(bytes, sizeof(int), cryptedLength);

            var decryptedBytes = new byte[decryptedData.Length + (bytes.Length - cryptedLength - sizeof(int))];

            Array.Copy(decryptedData, decryptedBytes, decryptedData.Length);
            Array.Copy(bytes, sizeof(int) + cryptedLength, decryptedBytes, decryptedData.Length, bytes.Length - cryptedLength - sizeof(int));

            EncryptProvider.DisposeEncrypter(decrypter);
            return decryptedBytes;
        }
        private IPacket GetResponsePacket(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var reader = new BinaryReader(ms);

            var packetType = GlobalByteDictionary<Type>.Get(reader.ReadByteArray(),
                                                           b => Type.GetType(Compressor.Decompress(b)));
            var packet = (IPacket)PacketActivator.CreateInstance(packetType);

            packet.DeserializePacket(reader);
            packet.DeserializeProtectedCustomData(reader);
            packet.DeserializeUnprotectedCustomData(reader);

            return packet;
        }
    }
}
