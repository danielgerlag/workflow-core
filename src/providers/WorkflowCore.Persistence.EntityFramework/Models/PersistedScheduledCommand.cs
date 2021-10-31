using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WorkflowCore.Persistence.EntityFramework.Models
{    
    public class PersistedScheduledCommand
    {
        [Key]
        public long PersistenceId { get; set; }

        [MaxLength(200)]
        public string CommandName { get; set; }

        [MaxLength(500)]
        public string Data { get; set; }
        
        public long ExecuteTime { get; set; }
    }
}
