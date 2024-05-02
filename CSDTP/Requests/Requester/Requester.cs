using CSDTP.Cryptography.Providers;
using CSDTP.Requests.RequestHeaders;
using System.Net;
using AutoSerializer;
using CSDTP.Cryptography.Algorithms;
using CSDTP.Protocols;
using CSDTP.Protocols.Communicators;

namespace CSDTP.Requests
{
    public class Requester : IDisposable
    {
        public int ReplyPort => Communicator.ListenPort;

        private readonly ICommunicator Communicator;

        private RequestManager RequestManager = null!;
        private PacketManager PacketManager = null!;

        private bool isDisposed;

        private Requester(ICommunicator communicator)
        {
            Communicator = communicator;
            Communicator.DataAppear += ResponseAppear;
        }

        internal static async Task<Requester> Initialize(ICommunicator communicator, IEncryptProvider? encryptProvider = null, Type? customPacketType = null)
        {
            var requester = new Requester(communicator);
            requester.PacketManager = encryptProvider == null ? new PacketManager() : new PacketManager(encryptProvider);
            if (customPacketType != null)
                requester.RequestManager = new RequestManager(customPacketType);
            else
                requester.RequestManager = new RequestManager();
            await requester.Communicator.Start();
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
                    Communicator.DataAppear -= ResponseAppear;
                    Communicator.Dispose();
                    PacketManager?.Dispose();
                }
                isDisposed = true;
            }
        }

        private async void ResponseAppear(object? sender, DataInfo dataInfo)
        {
            if (dataInfo.Data.Length == 0)
                return;

            var decryptedData = await PacketManager.DecryptBytes(dataInfo.Data);
            if (decryptedData.Length == 0)
                return;

            var packet = PacketManager.GetPacketFromBytes(decryptedData);
            if (packet == null)
                return;

            packet.ReceiveTime = DateTime.UtcNow;
            packet.Source = dataInfo.From;

            RequestManager.ResponseAppear(packet);
        }

        public async Task<bool> SendAsync<TData>(TData data)
                                where TData : ISerializable<TData>, new()
        {
            var container = RequestManager.PackToContainer(data);
            container.RequestKind = RequesKind.Data;
            var packet = RequestManager.PackToPacket(container, -1);
            var encrypter = await PacketManager.GetEncrypter(packet);
            var packetBytes = PacketManager.GetBytes(packet);

            var cryptedPacketBytes = PacketManager.EncryptBytes(packetBytes.bytes, packetBytes.posToCrypt, encrypter);

            return await Communicator.SendBytes(cryptedPacketBytes);
        }
        public async Task<TResponse?> RequestAsync<TResponse, TRequest>(TRequest data, TimeSpan timeout, CancellationToken token)
                                      where TRequest : ISerializable<TRequest>, new()
                                      where TResponse : ISerializable<TResponse>, new()
        {
            try
            {
                //Упаковка объекта в контейнер
                var container = RequestManager.PackToContainer<TResponse, TRequest>(data);
                container.RequestKind = RequesKind.Request; //Тип запроса

                //Упаковка запроса в пакет
                var packet = RequestManager.PackToPacket(container, ReplyPort);

                //Получение объекта шифровальщика
                var encrypter = await PacketManager.GetEncrypter(packet);

                //Сериализация пакета в массив байтов
                var packetBytes = PacketManager.GetBytes(packet);

                //Шифрование байтов пакета
                var cryptedPacketBytes = PacketManager.EncryptBytes(packetBytes.bytes, packetBytes.posToCrypt, encrypter);

                //Добавление контенера в список ожидающих ответа
                if (!RequestManager.AddRequest(container))
                    return default;

                //Отправка байтов пакета
                await Communicator.SendBytes(cryptedPacketBytes);

                //Ожидание ответа
                var responsePacket = await RequestManager.GetResponseAsync(container, timeout, token);
                if (responsePacket == null)
                    return default;

                //Возврат объекта ответа
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
