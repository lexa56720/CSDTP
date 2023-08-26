using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    internal class ResponseError : ISerializable<ResponseError>
    {
        public static ResponseError Deserialize(BinaryReader reader)
        {
            return new ResponseError();
        }

        public void Serialize(BinaryWriter writer)
        {
            return;
        }
    }
}
