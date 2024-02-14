using CSDTP.Cryptography.Algorithms;
using CSDTP.Packets;

namespace CSDTP.Cryptography.Providers
{
    public class SimpleEncryptProvider : IEncryptProvider
    {
        public SimpleEncryptProvider(IEncrypter encrypter)
        {
            Encrypter = encrypter;
        }
        public SimpleEncryptProvider(IEncrypter encrypter,IEncrypter decrypter)
        {
            Encrypter = encrypter;
            Decrypter = decrypter;
        }
        public IEncrypter Encrypter { get; }
        public IEncrypter Decrypter { get; }

        private bool isDisposed;
        public void Dispose()
        {
            if (!isDisposed && !Encrypter.IsDisposed)
                Encrypter.Dispose();
            isDisposed = true;
        }

        public void DisposeEncrypter(IEncrypter encryptor)
        {
            return;
        }

        public IEncrypter? GetDecrypter(ReadOnlySpan<byte> bytes)
        {
            if (Decrypter != null)
                return Decrypter;
            return Encrypter;
        }

        public IEncrypter? GetEncrypter(IPacketInfo responsePacket, IPacketInfo? requestPacket = null)
        {
            return Encrypter;
        }
    }
}
