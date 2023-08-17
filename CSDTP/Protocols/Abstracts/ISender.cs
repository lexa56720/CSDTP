﻿using CSDTP.Cryptography.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Abstracts
{
    internal interface ISender : IDisposable
    {
        public IPEndPoint Destination { get; }

        public IEncryptProvider? EncryptProvider { get; set; }

        public int ReplyPort { get; }
        public bool IsAvailable { get; }
        public void Close();
        public Task<bool> Send<T>(T data) where T : ISerializable<T>;
    }
}
