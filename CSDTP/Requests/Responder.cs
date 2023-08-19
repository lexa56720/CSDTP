using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Protocols.Abstracts;
using CSDTP.Requests.RequestHeaders;
using CSDTP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
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

        private Dictionary<Type, Action<object, IPacketInfo>> GetHandlers { get; set; } = new Dictionary<Type, Action<object, IPacketInfo>>();
        private Dictionary<Type, Func<object, IPacketInfo, object>> PostHandlers { get; set; } = new Dictionary<Type, Func<object, IPacketInfo, object>>();


        private CompiledActivator Activator = new CompiledActivator();

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

        public void RegisterGetHandler<T>(Action<T, IPacketInfo> action)
        {
            GetHandlers.Add(typeof(T), new Action<object, IPacketInfo>((o, i) => action((T)o, i)));
        }
        public void RegisterPostHandler<T, U>(Func<T, IPacketInfo, U> action) where U : ISerializable<U>
        {
            PostHandlers.Add(typeof(T), new Func<object, IPacketInfo, object>((o, i) => action((T)o, i)));
        }

        public void RegisterGetHandler<T>(Action<T> action)
        {
            GetHandlers.Add(typeof(T), new Action<object, IPacketInfo>((o, i) => action((T)o)));
        }
        public void RegisterPostHandler<T, U>(Func<T, U> action) where U : ISerializable<U>
        {
            PostHandlers.Add(typeof(T), new Func<object, IPacketInfo, object>((o, i) => action((T)o)));
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

                if (request.RequestType == RequestType.Post && PostHandlers.TryGetValue(request.DataType, out var postHandler))
                    HandlePostRequest(packet, request, postHandler);

                else if (request.RequestType == RequestType.Get && GetHandlers.TryGetValue(request.DataType, out var getHandler))
                    HandleGetRequest(packet, request, getHandler);
            }
            catch (Exception e)
            {
                throw new Exception("REQUEST HANDLING FAIL", e);
            }

        }
        private void HandleGetRequest(IPacket packet, IRequestContainer request, Action<object, IPacketInfo> handler)
        {
            handler(request.DataObj, packet);
        }
        private void HandlePostRequest(IPacket packet, IRequestContainer request, Func<object, IPacketInfo, object> handler)
        {
            var responseObj = handler(request.DataObj, packet);
            if (responseObj == null)
                return;

            var genericType = responseObj.GetType();
            var responseType = typeof(RequestContainer<>).MakeGenericType(genericType);

            var response = GetResponse(responseType, responseObj, request.Id);

            Reply(response, new IPEndPoint(packet.Source, packet.ReplyPort));
        }
        private IRequestContainer GetResponse(Type responseType, object responseObject, Guid id)
        {
            var response = (IRequestContainer)Activator.CreateInstance(responseType);
            response.Id = id;
            response.DataType = responseObject.GetType();
            response.RequestType = RequestType.Response;
            response.DataObj = responseObject;
            return response;
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
