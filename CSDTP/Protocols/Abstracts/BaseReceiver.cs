using CSDTP.Cryptography.Providers;
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

namespace CSDTP.Protocols.Abstracts
{
    public abstract class BaseReceiver : IReceiver
    {
        public virtual bool IsReceiving { get; protected set; }

        protected CancellationTokenSource? TokenSource { get; set; }


        private protected QueueProcessor<Tuple<byte[], IPAddress>> ReceiverQueue;

        private CompiledActivator Activator=new();

        private GlobalByteDictionary<Type> PacketType = new();
        public IEncryptProvider? DecryptProvider { get; set; }

        public virtual int Port { get; }

        public event EventHandler<IPacket>? DataAppear;

        protected internal static ITrafficLimiter? TrafficLimiter { get; set; }

        public BaseReceiver(int port)
        {
            Port = port;
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 10, TimeSpan.FromMilliseconds(20));
        }
        public BaseReceiver(int port, IEncryptProvider decryptProvider)
        {
            Port = port;
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 10, TimeSpan.FromMilliseconds(20));
            DecryptProvider = decryptProvider;
        }
        public BaseReceiver()
        {
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 10, TimeSpan.FromMilliseconds(20));
        }
        public BaseReceiver(IEncryptProvider decrypter)
        {
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 10, TimeSpan.FromMilliseconds(20));
            DecryptProvider = decrypter;
        }


        public abstract void Dispose();

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

        protected virtual void OnDataAppear(IPacket packet)
        {
            DataAppear?.Invoke(this, packet);
        }

        protected IPacket GetPacket(byte[] bytes, IPAddress source)
        {
            try
            {
                using var reader = new BinaryReader(new MemoryStream(bytes));

                var type = PacketType.Get(reader.ReadByteArray(), b=> Type.GetType(Compressor.Decompress(b)));
                var name = type.FullName;
                var packet = (IPacket)Activator.CreateInstance(type);

                if (DecryptProvider != null)
                    packet.Deserialize(reader, DecryptProvider);
                else
                    packet.Deserialize(reader);

                packet.ReceiveTime = DateTime.Now;
              //  reader.BaseStream.Position = 0;
               // packet.Deserialize(reader, DecryptProvider);

                packet.Source = source;
                return packet;
            }
            catch (Exception ex)
            {
                throw new Exception("PACKET DESERIALIZATION ERROR", ex);
            }

        }
        protected bool IsAllowed(IPEndPoint ip)
        {
            return (TrafficLimiter == null) || (TrafficLimiter != null && TrafficLimiter.IsAllowed(ip.Address)) ;
        }

        private void HandleData(Tuple<byte[], IPAddress> packetInfo)
        {
            var packet = GetPacket(packetInfo.Item1, packetInfo.Item2);
            OnDataAppear(packet);
        }
    }
}
