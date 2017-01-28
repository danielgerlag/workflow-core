using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.EntityFramework.Models
{    
    public class PersistedExtensionAttribute
    {
        [Key]
        public long PersistenceId { get; set; }

        public long ExecutionPointerId { get; set; }

        [ForeignKey("ExecutionPointerId")]
        public PersistedExecutionPointer ExecutionPointer { get; set; }

        [MaxLength(100)]
        public string AttributeKey { get; set; }

        public string AttributeValue { get; set; }

    }
}
