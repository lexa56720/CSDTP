using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Cryptography.Algorithms
{
    public class AesEncrypter : IEncrypter
    {
        private Aes AES { get; set; }
        public CryptMethod CryptMethod => CryptMethod.Aes;

        private bool isDisposed;

        private object locker = new object();
        public AesEncrypter()
        {
            AES = Aes.Create();
        }

        public AesEncrypter(byte[] key, byte[] iV)
        {
            AES = Aes.Create();
            AES.Key = key;
            AES.IV = iV;
        }
        public AesEncrypter(byte[] key)
        {
            AES = Aes.Create();
            AES.Key = key;
        }
        public void Dispose()
        {
            if (!isDisposed)
                AES.Dispose();
            isDisposed = true;
        }

        public byte[] Crypt(byte[] data)
        {
            lock (locker)
            {
                AES.GenerateIV();
                using var encryptor = AES.CreateEncryptor(AES.Key, AES.IV);

                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

                msEncrypt.Write(AES.IV);
                csEncrypt.Write(data, 0, data.Length);
                csEncrypt.FlushFinalBlock();

                return msEncrypt.ToArray();
            }

        }

        public byte[] Decrypt(byte[] data)
        {
            lock (locker)
            {
                var iv = new byte[16];
                using MemoryStream msDecrypt = new MemoryStream(data);
                msDecrypt.ReadExactly(iv, 0, 16);

                using var decryptor = AES.CreateDecryptor(AES.Key, iv);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);

                using var brDecrypt = new BinaryReader(csDecrypt);

                return brDecrypt.ReadBytes(data.Length);
            }
        }



        public byte[] Crypt(byte[] data, int offset, int count)
        {
            lock (locker)
            {
                AES.GenerateIV();

                using var encryptor = AES.CreateEncryptor(AES.Key, AES.IV);
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

                msEncrypt.Write(AES.IV);

                csEncrypt.Write(data, offset, count);
                csEncrypt.FlushFinalBlock();

                var output = msEncrypt.ToArray();
                return output;
            }
        }

        public byte[] Decrypt(byte[] data, int offset, int count)
        {
            lock (locker)
            {
                var iv = new byte[16];
                using MemoryStream msDecrypt = new MemoryStream(data, offset, count);
                msDecrypt.ReadExactly(iv, 0, 16);

                using var decryptor = AES.CreateDecryptor(AES.Key, iv);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var brDecrypt = new BinaryReader(csDecrypt);

                var result = brDecrypt.ReadBytes(data.Length);

                return result;
            }
        }
    }
}
