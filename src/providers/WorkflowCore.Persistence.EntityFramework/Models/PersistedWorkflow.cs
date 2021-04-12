using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.EntityFramework.Models
{    
    public class PersistedWorkflow
    {
        [Key]
        public long PersistenceId { get; set; }

        [MaxLength(200)]
        public Guid InstanceId { get; set; }

        [MaxLength(200)]
        public string WorkflowDefinitionId { get; set; }

        public int Version { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(200)]
        public string Reference { get; set; }

        public virtual PersistedExecutionPointerCollection ExecutionPointers { get; set; } = new PersistedExecutionPointerCollection();

        public long? NextExecution { get; set; }

        public string Data { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? CompleteTime { get; set; }

        public WorkflowStatus Status { get; set; }
        
    }
}
