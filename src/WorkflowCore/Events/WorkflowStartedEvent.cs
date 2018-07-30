using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.EventBus.Abstractions;

namespace WorkflowCore.Events
{
    public class WorkflowStartedEvent : IntegrationEvent
    {
        public string WorkflowInstanceId { get; set; }
    }
}