using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Interface
{
    public interface ILifeCycleEventHub
    {
        Task PublishNotification(LifeCycleEvent evt);
        void Subscribe(Action<LifeCycleEvent> action);
        Task Start();
        Task Stop();
    }
}
