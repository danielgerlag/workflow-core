using System;
using System.Collections.Generic;

namespace WorkflowCore.Models
{
    public class ExecutionResult
    {
        public bool Proceed { get; set; }

        public object OutcomeValue { get; set; }

        public TimeSpan? SleepFor { get; set; }

        public object PersistenceData { get; set; }

        public string EventName { get; set; }

        public string EventKey { get; set; }

        public DateTime EventAsOf { get; set; }

        public List<object> BranchValues { get; set; } = new List<object>();

        public ExecutionResult()
        {
        }

        public ExecutionResult(object outcome)
        {
            Proceed = true;
            OutcomeValue = outcome;
        }

        public static ExecutionResult Outcome(object value)
        {
            return new ExecutionResult()
            {
                Proceed = true,
                OutcomeValue = value
            };
        }

        public static ExecutionResult Next()
        {
            return new ExecutionResult()
            {
                Proceed = true,
                OutcomeValue = null
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

        public static ExecutionResult Branch(List<object> branches, object persistenceData)
        {
            return new ExecutionResult()
            {
                Proceed = false,
                PersistenceData = persistenceData,
                BranchValues = branches
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

        public static ExecutionResult WaitForEvent(string eventName, string eventKey, DateTime effectiveDate)
        {
            return new ExecutionResult()
            {
                Proceed = false,
                EventName = eventName,
                EventKey = eventKey,
                EventAsOf = effectiveDate.ToUniversalTime()
            };
        }
    }
}
