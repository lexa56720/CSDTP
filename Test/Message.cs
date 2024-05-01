using AutoSerializer;
using AutoSerializerSourceGenerator;

namespace Test
{
    internal class Message : ISerializable<Message>
    {
        public string Text { get; set; }

        public byte[] data { get; set; } = [];
        public Message(string text)
        {
            Text = text;
        }

        public Message()
        {
            Text = "";
        }
    }
    internal class MessageResp : ISerializable<MessageResp>
    {
        public string Text { get; set; }

        public byte[] data { get; set; } = [];
        public MessageResp(string text)
        {
            Text = text;
        }

        public MessageResp()
        {
            Text = "";
        }
    }
}
