using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class EventPublication
    {
        public Guid Id { get; set; }

        public string WorkflowId { get; set; }

        public int StepId { get; set; }

        public string EventName { get; set; }

        public string EventKey { get; set; }

        public object EventData { get; set; }
    }
}
