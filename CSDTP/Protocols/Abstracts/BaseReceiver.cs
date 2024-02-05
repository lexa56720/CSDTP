﻿using CSDTP.Cryptography.Providers;
using CSDTP.DosProtect;
using CSDTP.Packets;
using CSDTP.Utils;
using CSDTP.Utils.Collections;
using CSDTP.Utils.Performance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CSDTP.Protocols.Abstracts
{
    internal abstract class BaseReceiver : IReceiver
    {
        public virtual bool IsReceiving { get; protected set; }

        protected CancellationTokenSource? TokenSource { get; set; }


        private protected QueueProcessor<(byte[] data, IPAddress ip)> ReceiverQueue;

        private CompiledActivator Activator = new();

        private GlobalByteDictionary<Type> PacketType = new();
        public IEncryptProvider? DecryptProvider { get; set; }

        public virtual int Port { get; }

        public event EventHandler<(IPAddress from, byte[] data)>? DataAppear;
        public BaseReceiver(int port)
        {
            Port = port;
            ReceiverQueue = new QueueProcessor<(byte[], IPAddress)>(HandleData, 10, TimeSpan.FromMilliseconds(20));
        }
        public BaseReceiver()
        {
            ReceiverQueue = new QueueProcessor<(byte[], IPAddress)>(HandleData, 10, TimeSpan.FromMilliseconds(20));
        }


        public virtual void Dispose()
        {
            Stop();
            if (TokenSource != null)
            {
                TokenSource.Cancel();
                TokenSource.Dispose();
            }
        }

        public virtual void Start()
        {
            if (IsReceiving)
                return;

            IsReceiving = true;
            ReceiverQueue.Start();

            TokenSource = new CancellationTokenSource();
            var token = TokenSource.Token;
            Task.Run(async () =>
            {
                await ReceiveWork(token);
            });
        }

        protected abstract Task ReceiveWork(CancellationToken token);

        public virtual void Stop()
        {
            if (!IsReceiving)
                return;

            IsReceiving = false;
            ReceiverQueue.Stop();
        }

        protected virtual void OnDataAppear(byte[] bytes, IPAddress ip)
        {
            DataAppear?.Invoke(this, (ip, bytes));
        }

        private void HandleData((byte[] data, IPAddress ip) packetInfo)
        {
            OnDataAppear(packetInfo.data, packetInfo.ip);
        }
    }
}
