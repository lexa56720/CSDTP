using CSDTP.Protocols.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.DosProtect
{
    public class Configurator
    {
        public static void SetTrafficLimiter(ITrafficLimiter limiter)
        {
            BaseReceiver.TrafficLimiter = limiter;
        }
    }
}
