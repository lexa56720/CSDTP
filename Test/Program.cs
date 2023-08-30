using CSDTP;
using CSDTP.Cryptography;
using CSDTP.Cryptography.Algorithms;
using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Requests;
using CSDTP.Utils;
using System.Diagnostics;
using System.Net;
using System.Reflection.PortableExecutable;

namespace Test
{
    public class ShitPacket<T> : Packet<T> where T : ISerializable<T>
    {
        public ShitPacket()
        {
        }

        public ShitPacket(T data) : base(data)
        {
        }

        public int Shit { get; set; } = 10;

        protected override void DeserializeCustomData(BinaryReader reader)
        {
            Shit = reader.ReadInt32();
        }

        protected override void SerializeCustomData(BinaryWriter writer)
        {
            writer.Write(Shit);
        }
    }

    internal class Program
    {
        public static Protocol protocol = Protocol.Http;
        static async Task Main(string[] args)
        {
            //await CSDTP.Utils.PortUtils.PortForward(8888, "fff");


            // await TestGet();
            await TestPost();
            Console.ReadLine();

        }
        public static async Task TestGet()
        {
            using var requester = new Requester(new IPEndPoint(IPAddress.Loopback, 6666), 7777, protocol);
            using var responder = new Responder(TimeSpan.FromSeconds(-10), 6666, protocol);
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
            //using var crypter = new RsaEncrypter();
            using var crypter = new SimpleEncryptProvider(new AesEncrypter());


            //var port = PortUtils.GetFreePort() ;
            var port = 8080;

            using var responder = new Responder(TimeSpan.FromSeconds(10), port, crypter, crypter, protocol);
            responder.SetPacketType(typeof(ShitPacket<>));
            responder.RegisterPostHandler<Message, Message>(Modify);
            responder.Start();


            using var requester = new Requester(new IPEndPoint(IPAddress.Loopback, port), crypter, crypter, protocol);
            requester.SetPacketType(typeof(ShitPacket<>));




            int count = 0;
            int globalCount = 1;
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (globalCount < 10000)
            {
                // if (requester.Requests.Count < 50)
                // requester.PostAsync<Message, Message>(new Message("HI WORLD !"), TimeSpan.FromSeconds(2000)).ContinueWith(e=>Interlocked.Increment(ref count));


                var result = await requester.PostAsync<Message, Message>(new Message("HI WORLD !"), TimeSpan.FromSeconds(5)).ContinueWith(e => Interlocked.Increment(ref count));

                //Console.WriteLine(result.Text);
                //if (stopwatch.ElapsedMilliseconds > globalCount*1000)
                {
                    Console.Clear();
                    Console.WriteLine(1000 * (float)count / stopwatch.ElapsedMilliseconds + " " + 1000 * (float)counter / stopwatch.ElapsedMilliseconds);
                    //count =0;
                    // stopwatch.Restart();
                    globalCount++;
                }

            }
        }

        static int counter = 0;
        static Message Modify(Message msg, IPacketInfo info)
        {
            return new Message(msg.Text + " " + Interlocked.Increment(ref counter));
        }
        static void ModifyGet(Message msg, IPacketInfo info)
        {

        }
    }
}