using CSDTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    internal class Message : ISerializable<Message>
    {
        public string Text { get; }
        public Message(string text)
        {
            Text = text;
        }

        public Message()
        {
            Text = "";
        }

        public static Message Deserialize(BinaryReader reader)
        {
           return new Message(reader.ReadString());
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Text);
        }
    }
}
