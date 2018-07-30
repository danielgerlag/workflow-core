using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.EventBus.Abstractions;

namespace WorkflowCore.Events
{
    public class WorkflowCompleteEvent : IntegrationEvent
    {
        public string WorkflowInstanceId { get; set; }
    }
}