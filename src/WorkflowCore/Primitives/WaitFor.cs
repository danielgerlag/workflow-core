using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class WaitFor : StepBody
    {
        public string EventKey { get; set; }

        public string EventName { get; set; }

        public DateTime EffectiveDate { get; set; }

        public object EventData { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (!context.ExecutionPointer.EventPublished)
            {
                return ExecutionResult.WaitForEvent(EventName, EventKey, EffectiveDate);
            }

            EventData = context.ExecutionPointer.EventData;
            return ExecutionResult.Next();
        }
    }
}
