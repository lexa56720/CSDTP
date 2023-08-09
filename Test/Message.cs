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
        public static Message Deserialize(StreamReader reader)
        {
            throw new NotImplementedException();
        }

        public void Serialize(StreamWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
