using System;

namespace WorkflowCore.Models
{
    public class ExecutionError
    {
        public DateTime ErrorTime { get; set; }

        public string WorkflowId { get; set; }

        public string ExecutionPointerId { get; set; }

        public string Message { get; set; }
    }
}
