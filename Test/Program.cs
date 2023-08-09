using CSDTP;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Notifier notificator = new Notifier();

            notificator.Subscribe<Message>(Notificator);
            notificator.Unsubscribe<Message>(Notificator);

            Console.WriteLine("Hello, World!");
        }

        private static void Notificator(Message message)
        {
            throw new NotImplementedException();
        }
    }
}