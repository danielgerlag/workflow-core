using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class SubscriptionStep<TStepBody> : WorkflowStep<TStepBody>, ISubscriptionStep<TStepBody>
        where TStepBody : SubscriptionStepBody
    {
        public string EventKey { get; set; }

        public string EventName { get; set; }
    }
}
