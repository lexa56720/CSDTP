﻿using AutoSerializer;
using CSDTP.Cryptography.Algorithms;
using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Requests.RequestHeaders;
using PerformanceUtils.Performance;

namespace CSDTP.Requests
{
    internal class PacketManager : IDisposable
    {
        private IEncryptProvider? EncryptProvider;

        private bool isDisposed;

        public PacketManager(IEncryptProvider encryptProvider)
        {
            EncryptProvider = encryptProvider;
        }
        public PacketManager()
        {
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
                    EncryptProvider?.Dispose();
                }
                isDisposed = true;
            }
        }

        public (byte[] bytes, int posToCrypt) GetBytes(IPacket packet)
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
        public async Task<IEncrypter?> GetEncrypter(IPacketInfo responsePacket, IPacketInfo? requestPacket = null)
        {
            if (EncryptProvider == null)
                return null;
            return await EncryptProvider.GetEncrypter(responsePacket, requestPacket);
        }
        public byte[] EncryptBytes(byte[] bytes, int cryptedPos, IEncrypter? encrypter)
        {
            if (encrypter == null)
                return bytes;

            var crypted = encrypter.Crypt(bytes, 0, cryptedPos);

            var result = new byte[sizeof(int) + (bytes.Length - cryptedPos) + crypted.Length];
            Array.Copy(BitConverter.GetBytes(crypted.Length), result, sizeof(int));
            Array.Copy(crypted, 0, result, sizeof(int), crypted.Length);
            Array.Copy(bytes, cryptedPos, result, sizeof(int) + crypted.Length, bytes.Length - cryptedPos);

            EncryptProvider.DisposeEncrypter(encrypter);
            return result;
        }

        public async Task<byte[]> DecryptBytes(byte[] bytes)
        {
            if (EncryptProvider == null)
                return bytes;
            var cryptedLength = BitConverter.ToInt32(bytes, 0);

            var decrypter =await EncryptProvider.GetDecrypter(new Memory<byte>(bytes, cryptedLength, bytes.Length - cryptedLength));
            if (decrypter == null)
                return bytes;

            try
            {
                var decryptedData = decrypter.Decrypt(bytes, sizeof(int), cryptedLength);
                var decryptedBytes = new byte[decryptedData.Length + (bytes.Length - cryptedLength - sizeof(int))];

                Array.Copy(decryptedData, decryptedBytes, decryptedData.Length);
                Array.Copy(bytes, sizeof(int) + cryptedLength, decryptedBytes, decryptedData.Length, bytes.Length - cryptedLength - sizeof(int));

                EncryptProvider.DisposeEncrypter(decrypter);
                return decryptedBytes;
            }
            catch
            {
                return [];
            }
        }
        public IPacket<IRequestContainer>? GetPacketFromBytes(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var reader = new BinaryReader(ms);

            var packetType = GlobalByteDictionary<Type>.Get(reader.ReadByteArray(),
                                                           b => Type.GetType(Compressor.Decompress(b)));
            if (packetType == null)
                return null;

            var packet = (IPacket)CompiledActivator.CreateInstance(packetType);

            packet.DeserializePacket(reader);
            packet.DeserializeProtectedCustomData(reader);
            packet.DeserializeUnprotectedCustomData(reader);

            return packet as IPacket<IRequestContainer>;
        }
    }
}
