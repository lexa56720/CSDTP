using CSDTP.Cryptography.Algorithms;
using CSDTP.Packets;

namespace CSDTP.Cryptography.Providers
{
    public interface IEncryptProvider : IDisposable
    {
        public void DisposeEncrypter(IEncrypter encrypter);
        public Task<IEncrypter?> GetEncrypter(IPacketInfo responsePacket, IPacketInfo? requestPacket=null);
        public Task<IEncrypter?> GetDecrypter(ReadOnlySpan<byte> bytes);
    }
}
