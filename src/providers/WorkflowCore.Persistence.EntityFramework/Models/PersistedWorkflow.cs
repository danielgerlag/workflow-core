using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
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

        /// <summary>
        /// A count of how many <see cref="ExecutionError"/>'s have been generated against this <see cref="WorkflowInstance"/>.
        /// Errors can be retrieved separately due to the high amount of errors that may be generated.
        /// </summary>
        public int ExecutionErrorCount { get; set; }

    }
}
