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

        public IEncrypter Encrypter { get; }
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
            return Encrypter;
        }

        public IEncrypter? GetEncrypter(IPacketInfo packet)
        {
            return Encrypter;
        }
    }
}
