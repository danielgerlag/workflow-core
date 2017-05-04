using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Models
{
    public class ExecutionResult
    {
        public bool Proceed { get; set; }

        public object[] OutcomeValues { get; set; } = new object[0];

        public TimeSpan? SleepFor { get; set; }

        public object PersistenceData { get; set; }

        public ExecutionResult()
        {
        }

        public ExecutionResult(object outcome)
        {
            Proceed = true;
            OutcomeValues = new[] { outcome };
        }

        public static ExecutionResult Outcome(object value)
        {
            return new ExecutionResult()
            {
                Proceed = true,
                OutcomeValues = new[] { value }
            };
        }

        public static ExecutionResult Next()
        {
            return new ExecutionResult()
            {
                Proceed = true,
                OutcomeValues = new object[] { null }
            };
        }

        public static ExecutionResult Persist(object persistenceData)
        {
            return new ExecutionResult()
            {
                Proceed = false,
                PersistenceData = persistenceData
            };
        }

        public static ExecutionResult Sleep(TimeSpan duration, object persistenceData)
        {
            return new ExecutionResult()
            {
                Proceed = false,
                SleepFor = duration,
                PersistenceData = persistenceData
            };
        }

    }
}
