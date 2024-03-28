using AutoSerializer;

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
}
