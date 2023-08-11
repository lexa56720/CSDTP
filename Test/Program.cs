using CSDTP;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Notifier notificator = new Notifier();

            //notificator.Subscribe<Message>(Notificator);
            //notificator.Unsubscribe<Message>(Notificator);
            var stream = new MemoryStream();
            var packet = new Packet<Message>(new Message("FF"));
            packet.Serialize(new BinaryWriter(stream));


            Console.WriteLine("Hello, World!");
        }

        private static void Notificator(Message message)
        {
            throw new NotImplementedException();
        }
    }
}