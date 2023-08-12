using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Utils
{
    internal class LifeTimeController<T>
    {

        public Dictionary<T,DateTime> Objects = new Dictionary<T,DateTime>();


    }
}
