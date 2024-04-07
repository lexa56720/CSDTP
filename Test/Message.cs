﻿using AutoSerializer;
using AutoSerializerSourceGenerator;

namespace Test
{
    [ManualSerialization]
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
