using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.EntityFramework.Models
{    
    public class PersistedExecutionError
    {
        [Key]
        public long PersistenceId { get; set; }

        public long ExecutionPointerId { get; set; }

        [ForeignKey("ExecutionPointerId")]
        public PersistedExecutionPointer ExecutionPointer { get; set; }

        [MaxLength(50)]
        public string Id { get; set; }

        public DateTime ErrorTime { get; set; }

        public string Message { get; set; }

    }
}
