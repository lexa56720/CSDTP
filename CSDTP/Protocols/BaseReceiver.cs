using CSDTP.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CSDTP.Protocols
{
    public abstract class BaseReceiver : IReceiver
    {
        public virtual bool IsReceiving { get; protected set; }

        protected ConcurrentQueue<byte[]> ReceiverQueue = new ConcurrentQueue<byte[]>();

        public int Port { get; }

        public event EventHandler<IPacket> DataAppear;

        public BaseReceiver(int port)
        {
            Port = port;
        }

        public abstract void Dispose();

        public virtual void Start()
        {
            IsReceiving = true;
            HandleQueue();
        }

        public virtual void Stop()
        {
            IsReceiving = false;
        }

        protected virtual void OnDataAppear(IPacket packet)
        {
            DataAppear?.Invoke(this, packet);
        }
        protected IPacket GetPacket(byte[] bytes)
        {
            using var reader = new BinaryReader(new MemoryStream(bytes));
            var packet = (IPacket)Activator.CreateInstance(Type.GetType(reader.ReadString()));
            packet.Deserialize(reader);
            return packet;
        }

        private void HandleQueue()
        {
            Task.Run(async () =>
            {
                while (IsReceiving)
                {
                    if (ReceiverQueue.Count > 100)
                    {
                        Parallel.For(0, ReceiverQueue.Count, (i) =>
                        {
                            if (ReceiverQueue.TryDequeue(out var data))
                            {
                                var packet = GetPacket(data);
                                OnDataAppear(packet);
                            }
                        });
                    }
                    else if (ReceiverQueue.Count < 100 && ReceiverQueue.Count>0)
                    {
                        for (int i = 0; i < ReceiverQueue.Count; i++)
                            if (ReceiverQueue.TryDequeue(out var data))
                            {
                                var packet = GetPacket(data);
                                OnDataAppear(packet);
                            }
                    }
                    else
                    {
                        await Task.Delay(20);
                    }
                    
                }
            });
        }
    }
}
