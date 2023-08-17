using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using CSDTP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    public class Responder : IDisposable
    {
        private IEncryptProvider? EncryptProvider { get; init; }

        public bool IsTcp { get; }
        public int ListenPort => Receiver.Port;
        public bool IsRunning => Receiver.IsReceiving && RequestsQueue.IsRunning;

        private Dictionary<Type, object> GetHandlers { get; set; } = new Dictionary<Type, object>();
        private Dictionary<Type, object> PostHandlers { get; set; } = new Dictionary<Type, object>();

        private QueueProcessor<IPacket> RequestsQueue { get; set; }

        private LifeTimeController<ISender> Senders { get; set; }
        private IReceiver Receiver { get; set; }

        private MethodInfo SendMethod = typeof(ISender).GetMethods().First(m => m.Name == nameof(ISender.Send));
        public Responder(TimeSpan sendersTimeout, int port, bool isTcp = false)
        {
            Senders = new LifeTimeController<ISender>(sendersTimeout);
            RequestsQueue = new QueueProcessor<IPacket>(HandleRequest, 5, TimeSpan.FromMilliseconds(20));
            Receiver = new Receiver(port < 0 ? 0 : port, isTcp);
            Receiver.DataAppear += RequestAppear;
            IsTcp = isTcp;
        }
        public Responder(TimeSpan sendersTimeout, int port, IEncryptProvider encryptProvider, bool isTcp = false)
        {
            EncryptProvider = encryptProvider;

            Senders = new LifeTimeController<ISender>(sendersTimeout);
            RequestsQueue = new QueueProcessor<IPacket>(HandleRequest, 5, TimeSpan.FromMilliseconds(20));


            Receiver = new Receiver(port < 0 ? 0 : port, encryptProvider, isTcp);
            Receiver.DataAppear += RequestAppear;

            IsTcp = isTcp;
        }
        public Responder(TimeSpan sendersTimeout, int port, IEncryptProvider encrypterProvider, IEncryptProvider decryptProvider, bool isTcp = false)
        {
            EncryptProvider = encrypterProvider;

            Senders = new LifeTimeController<ISender>(sendersTimeout);
            RequestsQueue = new QueueProcessor<IPacket>(HandleRequest, 5, TimeSpan.FromMilliseconds(20));


            Receiver = new Receiver(port < 0 ? 0 : port, decryptProvider, isTcp);
            Receiver.DataAppear += RequestAppear;

            IsTcp = isTcp;
        }

        public void Dispose()
        {
            Senders.Stop();
            Receiver.Stop();
            RequestsQueue.Stop();

            Receiver.DataAppear -= RequestAppear;
            Receiver.Dispose();
        }

        public void Start()
        {
            if (IsRunning)
                return;

            Senders.Start();
            Receiver.Start();
            RequestsQueue.Start();
        }
        public void Stop()
        {
            if (!IsRunning)
                return;

            Senders.Stop();
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

        public void RegisterGetHandler<T>(Action<T, IPacketInfo> action)
        {
            GetHandlers.Add(typeof(T), action);
        }
        public void RegisterPostHandler<T, U>(Func<T, IPacketInfo, U> action) where U : ISerializable<U>
        {
            PostHandlers.Add(typeof(T), action);
        }

        private void RequestAppear(object? sender, IPacket e)
        {
            RequestsQueue.Add(e);
        }
        private void HandleRequest(IPacket packet)
        {
            try
            {
                var request = (IRequestContainer)packet.DataObj;

                if (request.RequestType == RequestType.Post && PostHandlers.TryGetValue(request.DataType, out object handlerFunc))
                    HandlePostRequest(packet, request, handlerFunc);

                else if (request.RequestType == RequestType.Get && GetHandlers.TryGetValue(request.DataType, out handlerFunc))
                    HandleGetRequest(packet, request, handlerFunc);
            }
            catch (Exception e)
            {
                throw new Exception("REQUEST HANDLING FAIL", e);
            }

        }
        private void HandlePostRequest(IPacket packet, IRequestContainer request, object handler)
        {
            var responseObj = InvokeMethod(request,packet,handler);
            if (responseObj == null)
                return;

            var genericType = responseObj.GetType();
            var responseType = typeof(RequestContainer<>).MakeGenericType(genericType);

            var response = (IRequestContainer)Activator.CreateInstance(responseType, responseObj, request.Id, RequestType.Response);
            Reply(response, new IPEndPoint(packet.Source, packet.ReplyPort));
        }
        private void HandleGetRequest(IPacket packet, IRequestContainer request,object handler)
        {
            InvokeMethod(request, packet, handler);
        }

        private object? InvokeMethod(IRequestContainer request, IPacketInfo packetInfo, object handler)
        {
            var method = ((Delegate)handler).Method;
            if (method.GetParameters().Length == 1)
                return method.Invoke(handler, new object[] { request.DataObj });
            else
                return method.Invoke(handler, new object[] { request.DataObj,packetInfo });

        }

        private void Reply(IRequestContainer data, IPEndPoint destination)
        {
            var sender = Senders.Get(s => s.Destination.Equals(destination) && s.IsAvailable);
            if (sender == null)
            {
                sender = GetNewSender(destination);
                Senders.Add(sender);
            }
            SendMethod.MakeGenericMethod(data.GetType()).Invoke(sender, new object[] { data });
        }

        private ISender GetNewSender(IPEndPoint destination)
        {
            if (EncryptProvider != null)
                return new Sender(destination, EncryptProvider, IsTcp);
            else
                return new Sender(destination, IsTcp);
        }
    }
}
