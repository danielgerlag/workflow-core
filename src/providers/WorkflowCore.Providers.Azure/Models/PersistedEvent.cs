using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.Azure.Models
{
    public class PersistedEvent
    {
        public string id { get; set; }

        public string EventName { get; set; }

        public string EventKey { get; set; }

        public string EventData { get; set; }

        public DateTime EventTime { get; set; }

        public bool IsProcessed { get; set; }

        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        public static PersistedEvent FromInstance(Event instance)
        {
            return new PersistedEvent()
            {
                id = instance.Id,
                EventKey = instance.EventKey,
                EventName = instance.EventName,
                EventTime = instance.EventTime,
                IsProcessed = instance.IsProcessed,
                EventData = JsonConvert.SerializeObject(instance.EventData, SerializerSettings),
            };
        }

        public static Event ToInstance(PersistedEvent instance)
        {
            return new Event()
            {
                Id = instance.id,
                EventKey = instance.EventKey,
                EventName = instance.EventName,
                EventTime = instance.EventTime,
                IsProcessed = instance.IsProcessed,
                EventData = JsonConvert.DeserializeObject(instance.EventData, SerializerSettings),
            };
        }
    }
}
