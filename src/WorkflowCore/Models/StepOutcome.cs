using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class StepOutcome
    {
        public object Value { get; set; }
        
        public int NextStep { get; set; }
    }
}
