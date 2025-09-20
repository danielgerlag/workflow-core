using System;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.Azure.Models
{
    public class ScheduledCommandTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string CommandName { get; set; }
        public string Data { get; set; }
        public long ExecuteTime { get; set; }

        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public static ScheduledCommandTableEntity FromInstance(ScheduledCommand instance)
        {
            return new ScheduledCommandTableEntity
            {
                PartitionKey = "command",
                RowKey = Guid.NewGuid().ToString(),
                CommandName = instance.CommandName,
                Data = instance.Data,
                ExecuteTime = instance.ExecuteTime,
            };
        }

        public static ScheduledCommand ToInstance(ScheduledCommandTableEntity entity)
        {
            return new ScheduledCommand
            {
                CommandName = entity.CommandName,
                Data = entity.Data,
                ExecuteTime = entity.ExecuteTime,
            };
        }
    }
}