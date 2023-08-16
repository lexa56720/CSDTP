using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Cryptography
{
    public class AesEncrypter : IEncrypter
    {
        private Aes AES { get; set; }

        public byte[] Key
        {
            get => AES.Key;
            set
            {
                AES.Key = value;
            }
        }

        public byte[] IV
        {
            get => AES.IV;
            set
            {
                AES.IV = value;
            }
        }

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
            AES.Dispose();
        }
        public byte[] Crypt(byte[] data)
        {
            AES.GenerateIV();
            using var encryptor = AES.CreateEncryptor(AES.Key, AES.IV);

            using MemoryStream msEncrypt = new MemoryStream();
            using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

            msEncrypt.Write(AES.IV);
            csEncrypt.Write(data, 0, data.Length);
            csEncrypt.FlushFinalBlock();
            var output = msEncrypt.ToArray();
            return output;
        }

        public byte[] Decrypt(byte[] data)
        {
            var iv = new byte[16];
            using MemoryStream msDecrypt = new MemoryStream(data);
            msDecrypt.ReadExactly(iv, 0, 16);
            IV = iv;

            using var decryptor = AES.CreateDecryptor(AES.Key, AES.IV);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);


            using BinaryReader brDecrypt = new BinaryReader(csDecrypt);
            var result = brDecrypt.ReadBytes(data.Length);

            return result;
        }



        public byte[] Crypt(byte[] data, int offset, int count)
        {
            AES.GenerateIV();
            using var encryptor = AES.CreateEncryptor(AES.Key, AES.IV);

            using MemoryStream msEncrypt = new MemoryStream();
            using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

            msEncrypt.Write(AES.IV);

            csEncrypt.Write(data, offset, count);
            csEncrypt.FlushFinalBlock();

            var output = msEncrypt.ToArray();
            return output;
        }

        public byte[] Decrypt(byte[] data, int offset, int count)
        {
            var iv = new byte[16];
            using MemoryStream msDecrypt = new MemoryStream(data, offset, count);
            msDecrypt.ReadExactly(iv, 0, 16);
            IV = iv;

            using var decryptor = AES.CreateDecryptor(AES.Key, AES.IV);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);


            using BinaryReader brDecrypt = new BinaryReader(csDecrypt);
            var result = brDecrypt.ReadBytes(data.Length);

            return result;
        }
    }
}
