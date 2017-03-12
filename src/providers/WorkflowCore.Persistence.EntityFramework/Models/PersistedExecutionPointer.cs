using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
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

        public string EventName { get; set; }

        public string EventKey { get; set; }

        public bool EventPublished { get; set; }

        public string EventData { get; set; }
        
        public int ConcurrentFork { get; set; }

        public bool PathTerminator { get; set; }

        public string StepName { get; set; }

        public List<PersistedExecutionError> Errors { get; set; } = new List<PersistedExecutionError>();

        public List<PersistedExtensionAttribute> ExtensionAttributes { get; set; } = new List<PersistedExtensionAttribute>();                

    }
}
