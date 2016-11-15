using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    public interface ISubscriptionBody : IStepBody
    {
        object EventData { get; set; }        
    }
}
