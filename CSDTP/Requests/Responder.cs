using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    internal class Responder
    {

        private QueueProcessor<IPacket> RequestsQueue;
        private Dictionary<IPEndPoint, ISender> Senders { get; set; }
        private IReceiver Receiver { get; set; }

        public bool IsRunning => Receiver.IsReceiving && RequestsQueue.IsRunning;

        private Dictionary<Type, object> GetHandlers { get; set; }

        private Dictionary<Type, object> PostHandlers { get; set; }

        public Responder(IPEndPoint destination, bool isTcp = false)
        {
            RequestsQueue = new QueueProcessor<IPacket>(RequestHandle, 20, TimeSpan.FromMilliseconds(20));
            Receiver = new Receiver(PortUtils.GetPort(), isTcp);
            Receiver.DataAppear += RequestAppear;
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

        }
    }
}
