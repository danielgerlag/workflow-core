using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models
{
    
    public class ActivityResult
    {
        public enum StatusType { Success, Fail }
        public StatusType Status { get; set; }
        public object Data { get; set; }
    }
}
