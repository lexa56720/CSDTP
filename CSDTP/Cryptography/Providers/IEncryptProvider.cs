using CSDTP.Cryptography.Algorithms;
using CSDTP.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Cryptography.Providers
{
    public interface IEncryptProvider : IDisposable
    {
        public void DisposeEncrypter(IEncrypter encrypter);
        public IEncrypter? GetEncrypter(IPacketInfo packet);
        public IEncrypter? GetDecrypter(ReadOnlySpan<byte> bytes);
    }
}
