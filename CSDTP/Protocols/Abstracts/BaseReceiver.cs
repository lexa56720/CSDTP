using CSDTP.Cryptography;
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

        public IEncrypter? Decrypter { get; protected set; }

        public virtual int Port { get; }

        public event EventHandler<IPacket>? DataAppear;

        public BaseReceiver(int port)
        {
            Port = port;
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 100, TimeSpan.FromMilliseconds(20));
        }
        public BaseReceiver(int port, IEncrypter decrypter)
        {
            Port = port;
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 100, TimeSpan.FromMilliseconds(20));
            Decrypter = decrypter;
        }
        public BaseReceiver()
        {
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 100, TimeSpan.FromMilliseconds(20));
        }
        public BaseReceiver(IEncrypter decrypter)
        {
            ReceiverQueue = new QueueProcessor<Tuple<byte[], IPAddress>>(HandleData, 100, TimeSpan.FromMilliseconds(20));
            Decrypter = decrypter;
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
                bytes = TryToDecrypt(bytes);

                using var reader = new BinaryReader(new MemoryStream(bytes));

                var packet = (IPacket)Activator.CreateInstance(Type.GetType(reader.ReadString()));
                packet.Deserialize(reader);
                packet.ReceiveTime = DateTime.Now;
                packet.Source = source;
                return packet;
            }
            catch (Exception ex)
            {
                throw new Exception("PACKET DESERIALIZE ERROR", ex);
            }

        }

        private byte[] TryToDecrypt(byte[] data)
        {
            var isCrypted = data[0] == 1;
            if (isCrypted)
                try
                {
                    if (Decrypter != null)
                        return Decrypter.Decrypt(data, 1, data.Length - 1);
                    else
                        throw new Exception("UNKONW PACKET FORMAT/CRYPTED BUT DECRYPTOR IS NULL");
                }
                catch
                {
                    throw;
                }
            else
                return data.Skip(1).ToArray();
        }

        private void HandleData(Tuple<byte[], IPAddress> packetInfo)
        {
            var packet = GetPacket(packetInfo.Item1, packetInfo.Item2);
            OnDataAppear(packet);
        }
    }
}
