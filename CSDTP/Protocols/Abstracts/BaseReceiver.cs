using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CSDTP.Protocols.Abstracts
{
    public abstract class BaseReceiver : IReceiver
    {
        public virtual bool IsReceiving { get; protected set; }


        private protected QueueProcessor<Tuple<byte[], IPAddress>> ReceiverQueue;

        public IEncryptProvider? DecryptProvider { get; set; }

        public virtual int Port { get; }

        public event EventHandler<IPacket>? DataAppear;

        public BaseReceiver(int port)
        {
            Port = port;
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 100, TimeSpan.FromMilliseconds(20));
        }
        public BaseReceiver(int port, IEncryptProvider decryptProvider)
        {
            Port = port;
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 100, TimeSpan.FromMilliseconds(20));
            DecryptProvider = decryptProvider;
        }
        public BaseReceiver()
        {
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 100, TimeSpan.FromMilliseconds(20));
        }
        public BaseReceiver(IEncryptProvider decrypter)
        {
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 100, TimeSpan.FromMilliseconds(20));
            DecryptProvider = decrypter;
        }

        public abstract void Dispose();
        public abstract void Close();

        public virtual void Start()
        {
            if (IsReceiving)
                return;

            IsReceiving = true;
            ReceiverQueue.Start();
        }

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

                var packet = (IPacket)Activator.CreateInstance(Type.GetType(reader.ReadString()));

                if (DecryptProvider != null)
                    packet.Deserialize(reader, DecryptProvider);
                else
                    packet.Deserialize(reader);

                packet.ReceiveTime = DateTime.Now;
                packet.Source = source;
                return packet;
            }
            catch (Exception ex)
            {
                throw new Exception("PACKET DESERIALIZATION ERROR", ex);
            }

        }

        private void HandleData(Tuple<byte[], IPAddress> packetInfo)
        {
            var packet = GetPacket(packetInfo.Item1, packetInfo.Item2);
            OnDataAppear(packet);
        }
    }
}
