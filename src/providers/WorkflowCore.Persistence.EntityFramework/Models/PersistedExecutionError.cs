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

        [MaxLength(100)]
        public string WorkflowId { get; set; }

        [MaxLength(100)]
        public string ExecutionPointerId { get; set; }
        
        public DateTime ErrorTime { get; set; }

        /// <summary>
        /// Exception/Error Message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Exception Type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Exception Source.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Exception Stack Trace.
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// Exception Target Site Name.
        /// </summary>
        public string TargetSiteName { get; set; }

        /// <summary>
        /// Exception Target Site Module.
        /// </summary>
        public string TargetSiteModule { get; set; }
        
        /// <summary>
        /// Exception Help Link.
        /// </summary>
        public string HelpLink { get; set; }
    }
}
