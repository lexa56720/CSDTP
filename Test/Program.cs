using CSDTP;
using CSDTP.Protocols;
using CSDTP.Requests;
using System.Diagnostics;
using System.Net;

namespace Test
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await CSDTP.Utils.PortUtils.PortForward(8888, "fff");


            await TestGet();
            await TestPost();
            Console.ReadLine();

        }
        public static async Task TestGet()
        {
            using var requester = new Requester(new IPEndPoint(IPAddress.Loopback, 6666), 7777);
            using var responder = new Responder(TimeSpan.FromSeconds(-10), 6666);
            responder.RegisterGetHandler<Message>(ModifyGet);
            responder.Start();

            int count = 0;
            int globalCount = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (globalCount < 5)
            {
                var result = await requester.GetAsync(new Message("fff " + count++));

                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    Console.Clear();
                    Console.WriteLine(1000 * (float)count / stopwatch.ElapsedMilliseconds);
                    count = 0;
                    globalCount++;
                    stopwatch.Restart();
                }

            }
        }

        public static async Task TestPost()
        {
            using var requester = new Requester(new IPEndPoint(IPAddress.Loopback, 6666), 7777);
            using var responder = new Responder(TimeSpan.FromSeconds(-10), 6666);
            responder.RegisterPostHandler<Message, Message>(Modify);
            responder.Start();

            int count = 0;
            int globalCount = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (globalCount < 5)
            {
                var result = await requester.PostAsync<Message, Message>(new Message("fff " + count++), TimeSpan.FromSeconds(20));

                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    Console.Clear();
                    Console.WriteLine(1000 * (float)count / stopwatch.ElapsedMilliseconds);
                    count = 0;
                    globalCount++;
                    stopwatch.Restart();
                }

            }
        }

        static int counter = 0;
        static Message Modify(Message msg)
        {
            return new Message(msg.Text + " " + counter++);
        }
        static void ModifyGet(Message msg)
        {

        }
    }
}