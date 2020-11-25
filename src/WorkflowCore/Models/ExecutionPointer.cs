using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowCore.Models
{
    public class ExecutionPointer
    {
        private IReadOnlyCollection<string> _scope = new List<string>();

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

        public Dictionary<string, object> ExtensionAttributes { get; set; } = new Dictionary<string, object>();

        public string StepName { get; set; }

        public int RetryCount { get; set; }

        public List<string> Children { get; set; } = new List<string>();

        public object ContextItem { get; set; }

        public string PredecessorId { get; set; }

        public object Outcome { get; set; }

        public PointerStatus Status { get; set; } = PointerStatus.Legacy;

        public IReadOnlyCollection<string> Scope
        {
            get => _scope;
            set => _scope = new List<string>(value);
        }
    }

    public enum PointerStatus
    {
        Legacy = 0,
        Pending = 1,
        Running = 2,
        Complete = 3,
        Sleeping = 4,
        WaitingForEvent = 5,
        Failed = 6,
        Compensated = 7,
        Cancelled = 8,
        PendingPredecessor = 9
    }
}
