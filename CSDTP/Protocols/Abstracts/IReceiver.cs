﻿using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Protocols.Abstracts
{
    internal interface IReceiver : IDisposable
    {
        public event EventHandler<IPacket> DataAppear;
        public IEncryptProvider? DecryptProvider { get;  set; }
        public bool IsReceiving { get; }
        public int Port { get; }

        public void Start();

        public void Stop();

        public void Close();
    }
}
