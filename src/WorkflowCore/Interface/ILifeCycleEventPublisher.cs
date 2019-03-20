using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Interface
{
    public interface ILifeCycleEventPublisher : IBackgroundTask
    {
        void PublishNotification(LifeCycleEvent evt);
    }
}
