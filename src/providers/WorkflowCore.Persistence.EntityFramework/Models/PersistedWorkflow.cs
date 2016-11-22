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
        public long ClusterKey { get; set; }

        [MaxLength(200)]
        public Guid InstanceId { get; set; }

        [MaxLength(200)]
        public string WorkflowDefinitionId { get; set; }

        public int Version { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public string ExecutionPointers { get; set; }

        //[Index]
        public long? NextExecution { get; set; }

        public string Data { get; set; }

        public WorkflowStatus Status { get; set; }
        
    }
}
