using CSDTP.DosProtect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Requests
{
    public class ResponderPipeline
    {
        public ITrafficLimiter? TrafficLimiter { get; init; }
        protected bool IsAllowed(IPEndPoint ip)
        {
            return (TrafficLimiter == null) || (TrafficLimiter != null && TrafficLimiter.IsAllowed(ip.Address));
        }
    }
}
