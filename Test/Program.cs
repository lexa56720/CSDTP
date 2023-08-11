using CSDTP;
using CSDTP.Protocols;
using System.Net;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Notifier notificator = new Notifier();

            //notificator.Subscribe<Message>(Notificator);
            //notificator.Unsubscribe<Message>(Notificator);


            var receiver = new Receiver(6666);
            receiver.DataAppear += Receiver_DataAppear;
            receiver.Start();

            var sender = new Sender(new IPEndPoint(IPAddress.Loopback, 6666));
         
            while (true)
            {
                var msg = new Message("FF");
                sender.Send(msg);
                Thread.Sleep(100);
            }
          

            Console.ReadLine();
            Console.WriteLine("Hello, World!");
        }

        static int counter = 0;
        private static void Receiver_DataAppear(object? sender, CSDTP.Packets.IPacket e)
        {
            var msg = (Packet<Message>)e;
            Console.WriteLine(msg.Data.Text+" "+ counter);
            counter++;
        }

        private static void Notificator(Message message)
        {
            throw new NotImplementedException();
        }
    }
}