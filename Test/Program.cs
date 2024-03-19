using AutoSerializer;
using CSDTP.Cryptography.Algorithms;
using CSDTP.Cryptography.Providers;
using CSDTP.Packets;
using CSDTP.Protocols;
using CSDTP.Requests;
using System.Diagnostics;
using System.Net;

namespace Test
{
    public class ShitPacket<T> : Packet<T> where T : ISerializable<T>, new()
    {
        public ShitPacket()
        {
        }

        public ShitPacket(T data) : base(data)
        {
        }

        public int Shit { get; set; } = 10;

        public override void DeserializeUnprotectedCustomData(BinaryReader reader)
        {
            Shit = reader.ReadInt32();
        }

        public override void SerializeUnprotectedCustomData(BinaryWriter writer)
        {
            writer.Write(Shit);
        }
    }

    internal class Program
    {
        public static Protocol protocol = Protocol.Udp;
        static async Task Main(string[] args)
        {
            Serializer.SerializerProvider = new SerializerProvider();
            // var rp = new RequesterPipeline(new IPEndPoint(IPAddress.Loopback, 666), 667, Protocol.Udp);
            //await rp.SendRequestAsync<Message, Message>(new Message("HI"), TimeSpan.FromSeconds(5));
            //await CSDTP.Utils.PortUtils.PortForward(8888, "fff");


            // await TestGet();
            await TestPost();
            Console.ReadLine();

        }
        public static async Task TestGet()
        {
            //using var requester = new Requester(new IPEndPoint(IPAddress.Loopback, 6666), 7777, protocol);
            //using var responder = new Responder(TimeSpan.FromSeconds(-10), 6666, protocol);
            //responder.RegisterGetHandler<Message>(ModifyGet);
            //responder.Start();

            //int count = 0;
            //int globalCount = 0;
            //Stopwatch stopwatch = Stopwatch.StartNew();

            //while (globalCount < 5)
            //{
            //    var result = await requester.GetAsync(new Message("fff " + count++));

            //    if (stopwatch.ElapsedMilliseconds > 1000)
            //    {
            //        Console.Clear();
            //        Console.WriteLine(1000 * (float)count / stopwatch.ElapsedMilliseconds);
            //        count = 0;
            //        globalCount++;
            //        stopwatch.Restart();
            //    }

            //}
        }

        public static async Task TestPost()
        {
            //using var crypter = new RsaEncrypter();
            using var crypter = new SimpleEncryptProvider(new AesEncrypter());


            //var port = PortUtils.GetFreePort() ;
            var port = 250;
            var protocol = Protocol.Http;
            using var responder = ResponderFactory.Create(crypter, typeof(ShitPacket<>), protocol);
            responder.RegisterRequestHandler<Message, Message>(Modify);
            responder.Start();


            using var requester = RequesterFactory.Create(new IPEndPoint(IPAddress.Loopback, responder.ListenPort), port, crypter, typeof(ShitPacket<>), protocol);


            int count = 0;
            int globalCount = 1;
            int sended = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (globalCount < 100000)
            {
                // if (requester.Requests.Count < 50)
                // requester.PostAsync<Message, Message>(new Message("HI WORLD !"), TimeSpan.FromSeconds(2000)).ContinueWith(e=>Interlocked.Increment(ref count));

                var result =  requester.RequestAsync<Message, Message>(new Message("HI WORLD !"), TimeSpan.FromSeconds(5))
                                                                            .ContinueWith(e => Interlocked.Increment(ref count));
                //Interlocked.Increment(ref sended);
                //Console.WriteLine(result.Text);
                if (stopwatch.ElapsedMilliseconds > globalCount * 1000)
                {
                    Console.Clear();
                    Console.WriteLine(1000f * count / stopwatch.ElapsedMilliseconds + " " + 1000f * counter / stopwatch.ElapsedMilliseconds);
                    sended = 0;
                    count = 0;
                    counter = 0;
                  //  Interlocked.Exchange(ref sended, 0);
                    stopwatch.Restart();
                    globalCount++;
                }

            }
        }

        static int counter = 0;
        static Message Modify(Message msg, IPacketInfo info)
        {
            //Thread.Sleep(100);
            return new Message(msg.Text + " " + Interlocked.Increment(ref counter));
        }
        static void ModifyGet(Message msg, IPacketInfo info)
        {

        }

    }
}