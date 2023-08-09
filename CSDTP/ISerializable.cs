using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP
{
    public interface ISerializable<T>
    {
        public void Serialize(StreamWriter writer);


        public static abstract T Deserialize(StreamReader reader);
    }
}
