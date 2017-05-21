using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class SubscriptionStepBody : StepBody, ISubscriptionBody
    {

        public object EventData { get; set; }


        public SubscriptionStepBody()
        {
            
        }        

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            return OutcomeResult(null);
        }
    }
}
