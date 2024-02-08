﻿namespace CSDTP.Cryptography.Algorithms
{
    public enum CryptMethod
    {
        None,
        Rsa,
        Aes,
    }
    public interface IEncrypter : IDisposable
    {
        public CryptMethod CryptMethod { get; }

        public bool IsDisposed { get; }

        public byte[] Crypt(byte[] data);

        public byte[] Decrypt(byte[] data);

        public byte[] Crypt(byte[] data, int offset, int count);

        public byte[] Decrypt(byte[] data, int offset, int count);

    }
}
