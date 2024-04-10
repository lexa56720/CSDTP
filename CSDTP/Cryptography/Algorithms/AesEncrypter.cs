using System.Security.Cryptography;

namespace CSDTP.Cryptography.Algorithms
{
    public class AesEncrypter : IEncrypter
    {
        public byte[] Key { get; set; }

        public CryptMethod CryptMethod => CryptMethod.Aes;

        public bool IsDisposed { get; private set; }

        public AesEncrypter()
        {
            using var aes =Aes.Create();
            Key = new byte[aes.Key.Length];
            aes.Key.CopyTo(Key, 0);
        }
        public AesEncrypter(byte[] key)
        {
            Key = key;
        }
        public void Dispose()
        {
            if (!IsDisposed)
            {
                Array.Clear(Key);
            }
            IsDisposed = true;
        }

        public byte[] Crypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(Key, aes.IV);

            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

            msEncrypt.Write(aes.IV);
            csEncrypt.Write(data, 0, data.Length);
            csEncrypt.FlushFinalBlock();

            return msEncrypt.ToArray();
        }

        public byte[] Decrypt(byte[] data)
        {
            var iv = new byte[16];

            using var aes = Aes.Create();

            using MemoryStream msDecrypt = new MemoryStream(data);
            msDecrypt.ReadExactly(iv, 0, 16);

            using var decryptor = aes.CreateDecryptor(Key, iv);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);

            using var brDecrypt = new BinaryReader(csDecrypt);

            return brDecrypt.ReadBytes(data.Length);
        }

        public byte[] Crypt(byte[] data, int offset, int count)
        {
            using var aes = Aes.Create();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

            msEncrypt.Write(aes.IV);

            csEncrypt.Write(data, offset, count);
            csEncrypt.FlushFinalBlock();

            var output = msEncrypt.ToArray();
            return output;
        }

        public byte[] Decrypt(byte[] data, int offset, int count)
        {
            using var aes = Aes.Create();
            var iv = new byte[16];
            using var msDecrypt = new MemoryStream(data, offset, count);
            msDecrypt.ReadExactly(iv, 0, 16);

            using var decryptor = aes.CreateDecryptor(Key, iv);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var brDecrypt = new BinaryReader(csDecrypt);

            var result = brDecrypt.ReadBytes(data.Length);

            return result;
        }
    }
}
