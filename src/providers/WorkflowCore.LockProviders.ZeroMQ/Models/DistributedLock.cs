using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.LockProviders.ZeroMQ.Models
{
    public class DistributedLock
    {
        public Guid NodeId { get; set; }
        public string ResourceId { get; set; }
        public DateTime Expiry { get; set; }
    }
}
