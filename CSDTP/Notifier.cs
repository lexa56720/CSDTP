
namespace CSDTP
{

    public class Notifier
    {

        private Dictionary<Type, Action<object>> Subscribers = new Dictionary<Type, Action<object>>();


        public void Subscribe<T>(Action<T> action) where T : ISerializable<T>
        {
            Subscribers.Add(typeof(T), o => action((T)o));
        }

        public void Unsubscribe<T>(Action<T> action) where T : ISerializable<T>
        {
            if (Subscribers.ContainsKey(typeof(T)))
                Subscribers.Remove(typeof(T));
        }

        internal void PacketAppear<T>(Packet<T> packet) where T : ISerializable<T>
        {
            if (packet.Data == null) 
                return;

            if (Subscribers.TryGetValue(typeof(T), out var action))
                action(packet.Data);
        }
    }
}