using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models
{
    public class Event
    {
        public string Id { get; set; }        

        public string EventName { get; set; }

        public string EventKey { get; set; }

        public object EventData { get; set; }

        public DateTime CreateTime { get; set; }

        public bool IsProcessed { get; set; }
    }
}
