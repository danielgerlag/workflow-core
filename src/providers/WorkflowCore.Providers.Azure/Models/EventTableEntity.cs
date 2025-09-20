using System;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.Azure.Models
{
    public class EventTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string EventName { get; set; }
        public string EventKey { get; set; }
        public string EventData { get; set; }
        public DateTime EventTime { get; set; }
        public bool IsProcessed { get; set; }

        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public static EventTableEntity FromInstance(Event instance)
        {
            return new EventTableEntity
            {
                PartitionKey = "event",
                RowKey = instance.Id,
                EventName = instance.EventName,
                EventKey = instance.EventKey,
                EventTime = instance.EventTime,
                IsProcessed = instance.IsProcessed,
                EventData = JsonConvert.SerializeObject(instance.EventData, SerializerSettings),
            };
        }

        public static Event ToInstance(EventTableEntity entity)
        {
            return new Event
            {
                Id = entity.RowKey,
                EventName = entity.EventName,
                EventKey = entity.EventKey,
                EventTime = entity.EventTime,
                IsProcessed = entity.IsProcessed,
                EventData = JsonConvert.DeserializeObject(entity.EventData, SerializerSettings),
            };
        }
    }
}