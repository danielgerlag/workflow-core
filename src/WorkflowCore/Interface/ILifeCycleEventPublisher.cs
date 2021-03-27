using System;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Interface
{
    public interface ILifeCycleEventPublisher : IBackgroundTask
    {
        void PublishNotification(LifeCycleEvent evt);
    }
}
