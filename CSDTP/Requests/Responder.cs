using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    public class Responder
    {

        private QueueProcessor<IPacket> RequestsQueue;

        private LifeTimeController<ISender> Senders;
        private IReceiver Receiver { get; set; }

        private MethodInfo SendMethod = typeof(ISender).GetMethods().First(m => m.Name == nameof(ISender.Send));

        public bool IsRunning => Receiver.IsReceiving && RequestsQueue.IsRunning;

        private Dictionary<Type, object> GetHandlers { get; set; }

        private Dictionary<Type, object> PostHandlers { get; set; }

        public bool IsTcp { get; }

        public Responder(TimeSpan sendersTimeout, int port, bool isTcp = false)
        {
            Senders = new LifeTimeController<ISender>(sendersTimeout);
            RequestsQueue = new QueueProcessor<IPacket>(RequestHandle, 20, TimeSpan.FromMilliseconds(20));
            Receiver = new Receiver(port < 0 ? PortUtils.GetPort() : port, isTcp);
            Receiver.DataAppear += RequestAppear;
            IsTcp = isTcp;
        }


        public void Start()
        {
            if (IsRunning)
                return;

            Receiver.Start();
            RequestsQueue.Start();
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            Receiver.Stop();
            RequestsQueue.Stop();

        }

        public void RegisterGetHandler<T>(Action<T> action)
        {
            GetHandlers.Add(typeof(T), action);
        }
        public void RegisterPostHandler<T, U>(Func<T, U> action) where U : ISerializable<U>
        {
            PostHandlers.Add(typeof(T), action);
        }

        private void RequestAppear(object? sender, IPacket e)
        {
            RequestsQueue.Add(e);
        }
        private void RequestHandle(IPacket packet)
        {
            var request = (IRequestContainer)packet.DataObj;
            if (request.RequestType == RequestType.Post && PostHandlers.TryGetValue(request.DataType, out var handler))
            {
                var responseObj = ((Func<object, object>)handler).Invoke(request.DataObj);
                var response = (IRequestContainer)Activator.CreateInstance(typeof(RequestContainer<>), responseObj, request.Id, RequestType.Response);
                Reply(response, new IPEndPoint(packet.Source, packet.ReplyPort));
            }
        }
        private async Task Reply(IRequestContainer data, IPEndPoint destination)
        {
            var sender = Senders.Get(s => s.Destination.Equals(destination) && s.IsAvailable);
            if (sender == null)
            {
                sender = new Sender(destination, IsTcp);
                Senders.Add(sender);
            }
            SendMethod.Invoke(sender, new object[] { data });
        }
    }
}
