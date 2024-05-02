using System.Security.Cryptography;

namespace CSDTP.Cryptography.Algorithms
{
    public class RsaEncrypter : IEncrypter
    {
        private RSA RSA { get; set; }
        public bool IsDisposed { get; private set; }

        public RsaEncrypter()
        {
            RSA = RSA.Create(4096);
        }

        public RsaEncrypter(string key)
        {
            RSA = RSA.Create();
            RSA.FromXmlString(key);
        }

        public void Dispose()
        {
            if (!IsDisposed)
                RSA.Dispose();
            IsDisposed = true;
        }
        public string PublicKey
        {
            get => RSA.ToXmlString(false);
            set
            {
                RSA.FromXmlString(value);
            }
        }

        public string PrivateKey
        {
            get => RSA.ToXmlString(true);
            set
            {
                RSA.FromXmlString(value);
            }
        }


        public byte[] Crypt(byte[] data)
        {
            return RSA.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
        }

        public byte[] Decrypt(byte[] data)
        {
            return RSA.Decrypt(data, RSAEncryptionPadding.OaepSHA1);
        }

        public byte[] Crypt(byte[] data, int offset, int count)
        {
            return RSA.Encrypt(new ReadOnlySpan<byte>(data, offset, count), RSAEncryptionPadding.OaepSHA1);
        }

        public byte[] Decrypt(byte[] data, int offset, int count)
        {
            return RSA.Decrypt(new ReadOnlySpan<byte>(data, offset, count), RSAEncryptionPadding.OaepSHA1);
        }
    }
}
