using CSDTP.Cryptography.Algorithms;
using CSDTP.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Cryptography.Providers
{
    public class SimpleEncryptProvider : IEncryptProvider
    {

        public SimpleEncryptProvider(IEncrypter encrypter)
        {
            Encrypter = encrypter;
        }

        public IEncrypter Encrypter { get; }

        public void Dispose()
        {
            Encrypter.Dispose();
        }

        public IEncrypter GetDecrypter(IPacket packet)
        {
            return Encrypter;
        }

        public IEncrypter GetEncrypter(IPacket packet)
        {
            return Encrypter;
        }
    }
}
