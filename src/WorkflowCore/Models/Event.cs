using System;

namespace WorkflowCore.Models
{
    public class Event
    {
        public string Id { get; set; }        

        public string EventName { get; set; }

        public string EventKey { get; set; }

        public object EventData { get; set; }

        public DateTime EventTime { get; set; }

        public bool IsProcessed { get; set; }
        
        public const string EventTypeActivity = "WorkflowCore.Activity";
    }
}
