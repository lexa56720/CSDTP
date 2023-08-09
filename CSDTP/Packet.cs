using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP
{
    public class Packet<T> : ISerializable<T> where T : ISerializable<T>
    {

        public bool IsHasData;

        public T? Data;


        public static T Deserialize(StreamReader reader)
        {
            throw new NotImplementedException();
        }

        public void Serialize(StreamWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
