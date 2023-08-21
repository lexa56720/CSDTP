using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.DosProtect
{
    public interface ITrafficLimiter
    {
        public bool IsAllowed(IPAddress source);
    }
}
