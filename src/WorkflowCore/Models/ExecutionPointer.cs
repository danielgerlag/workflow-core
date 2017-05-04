using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Models
{
    public class ExecutionPointer
    {
        public string Id { get; set; }

        public int StepId { get; set; }

        public bool Active { get; set; }

        public DateTime? SleepUntil { get; set; }

        public object PersistenceData { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string EventName { get; set; }

        public string EventKey { get; set; }

        public bool EventPublished { get; set; }

        public object EventData { get; set; }

        public int ConcurrentFork { get; set; }

        public bool PathTerminator { get; set; }
        
        public Dictionary<string, object> ExtensionAttributes { get; set; } = new Dictionary<string, object>();

        public string StepName { get; set; }

        public int RetryCount { get; set; }

    }
}
