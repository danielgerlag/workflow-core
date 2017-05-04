using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Models
{
    public class ExecutionError
    {
        public string Id { get; set; }

        public DateTime ErrorTime { get; set; }

        public string WorkflowId { get; set; }

        public string ExecutionPointerId { get; set; }

        public string Message { get; set; }
    }
}
