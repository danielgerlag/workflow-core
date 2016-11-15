using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    public interface ISubscriptionStep 
    {
        string EventName { get; set; }
        string EventKey { get; set; }
    }

    public interface ISubscriptionStep<TStepBody> : ISubscriptionStep
        where TStepBody : ISubscriptionBody
    {

    }
}
