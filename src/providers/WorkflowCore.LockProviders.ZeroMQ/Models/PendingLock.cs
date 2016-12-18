using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.LockProviders.ZeroMQ.Models
{
    public class PendingLock
    {
        public string ResourceId { get; set; }
        //public int RequestCount { get; set; }
        public ConcurrentDictionary<Guid, bool> Responses { get; set; } = new ConcurrentDictionary<Guid, bool>();
        public Action Callback { get; set; }
    }
}
