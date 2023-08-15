using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Cryptography
{
    public interface IEncrypter : IDisposable
    {
        public byte[] Crypt(byte[] data);

        public byte[] Decrypt(byte[] data);

        public byte[] Crypt(byte[] data,int offset,int count);

        public byte[] Decrypt(byte[] data, int offset, int count);
    }
}
