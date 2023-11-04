using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Cryptography.Algorithms
{
    public class FakeEncrypter : IEncrypter
    {
        public CryptMethod CryptMethod => CryptMethod.None;

        public byte[] Crypt(byte[] data)
        {
            return data;
        }

        public byte[] Crypt(byte[] data, int offset, int count)
        {
            return new Memory<byte>(data, offset, count).ToArray();
        }

        public byte[] Decrypt(byte[] data)
        {
            return data;
        }

        public byte[] Decrypt(byte[] data, int offset, int count)
        {
            return new Memory<byte>(data, offset, count).ToArray();
        }

        public void Dispose()
        {
            return;
        }
    }
}
