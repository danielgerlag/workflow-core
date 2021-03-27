using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.EntityFramework.Models
{    
    public class PersistedExecutionPointer
    {
        [Key]
        public long PersistenceId { get; set; }

        public long WorkflowId { get; set; }

        [ForeignKey("WorkflowId")]
        public PersistedWorkflow Workflow { get; set; }

        [MaxLength(50)]
        public string Id { get; set; }

        public int StepId { get; set; }

        public bool Active { get; set; }

        public DateTime? SleepUntil { get; set; }

        public string PersistenceData { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [MaxLength(100)]
        public string EventName { get; set; }

        [MaxLength(100)]
        public string EventKey { get; set; }

        public bool EventPublished { get; set; }

        public string EventData { get; set; }

        [MaxLength(100)]
        public string StepName { get; set; }
                
        public List<PersistedExtensionAttribute> ExtensionAttributes { get; set; } = new List<PersistedExtensionAttribute>();

        public int RetryCount { get; set; }

        public string Children { get; set; }

        public string ContextItem { get; set; }

        [MaxLength(100)]
        public string PredecessorId { get; set; }

        public string Outcome { get; set; }

        public PointerStatus Status { get; set; } = PointerStatus.Legacy;

        public string Scope { get; set; }
    }
}
