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
        private ISender Sender { get; set; }
        private IReceiver Receiver { get; set; }

        private Dictionary<Type, object> GetHandlers { get; set; }

        private Dictionary<Type, object> PostHandlers { get; set; }

        public Responder(IPEndPoint destination, bool isTcp = false)
        {
            Sender = new Sender(destination, isTcp);
            Receiver = new Receiver(PortUtils.GetPort(), isTcp);
            Receiver.DataAppear += RequestAppear;
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
            var request = (IRequestContainer)e.DataObj;
            if (request.RequestType == RequestType.Post)
            {
                if (PostHandlers.TryGetValue(request.DataType, out var handler))
                {
                    ((Func<object, object>)handler).Invoke(request.DataObj);
                    var response = new RequestContainer<>();
                }
            }

        }
    }
}
