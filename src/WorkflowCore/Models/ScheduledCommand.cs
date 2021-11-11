using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models
{
    public class ScheduledCommand
    {
        public const string ProcessWorkflow = "ProcessWorkflow";
        public const string ProcessEvent = "ProcessEvent";

        public string CommandName { get; set; }
        public string Data { get; set; }
        public long ExecuteTime { get; set; }
    }
}
