using System;

namespace WorkflowCore.Models
{
    public class ExecutionError
    {
        public DateTime ErrorTime { get; set; }

        public string WorkflowId { get; set; }

        public string ExecutionPointerId { get; set; }

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
