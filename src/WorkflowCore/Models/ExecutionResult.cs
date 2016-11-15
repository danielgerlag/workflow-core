using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Models
{
    public class ExecutionResult
    {
        public bool Proceed { get; set; }

        public object OutcomeValue { get; set; }

        public DateTime? SleepUntil { get; set; }

        public object PersistenceData { get; set; }

        public ExecutionResult()
        {
        }

        public ExecutionResult(object outcome)
        {
            Proceed = true;
            OutcomeValue = outcome;
        }

    }
}
