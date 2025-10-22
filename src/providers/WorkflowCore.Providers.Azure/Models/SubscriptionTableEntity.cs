using System;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.Azure.Models
{
    public class SubscriptionTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string WorkflowId { get; set; }
        public int StepId { get; set; }
        public string ExecutionPointerId { get; set; }
        public string EventName { get; set; }
        public string EventKey { get; set; }
        public DateTime SubscribeAsOf { get; set; }
        public string SubscriptionData { get; set; }
        public string ExternalToken { get; set; }
        public string ExternalWorkerId { get; set; }
        public DateTime? ExternalTokenExpiry { get; set; }

        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public static SubscriptionTableEntity FromInstance(EventSubscription instance)
        {
            return new SubscriptionTableEntity
            {
                PartitionKey = "subscription",
                RowKey = instance.Id,
                WorkflowId = instance.WorkflowId,
                StepId = instance.StepId,
                ExecutionPointerId = instance.ExecutionPointerId,
                EventName = instance.EventName,
                EventKey = instance.EventKey,
                SubscribeAsOf = instance.SubscribeAsOf,
                ExternalToken = instance.ExternalToken,
                ExternalWorkerId = instance.ExternalWorkerId,
                ExternalTokenExpiry = instance.ExternalTokenExpiry,
                SubscriptionData = JsonConvert.SerializeObject(instance.SubscriptionData, SerializerSettings),
            };
        }

        public static EventSubscription ToInstance(SubscriptionTableEntity entity)
        {
            return new EventSubscription
            {
                Id = entity.RowKey,
                WorkflowId = entity.WorkflowId,
                StepId = entity.StepId,
                ExecutionPointerId = entity.ExecutionPointerId,
                EventName = entity.EventName,
                EventKey = entity.EventKey,
                SubscribeAsOf = entity.SubscribeAsOf,
                ExternalToken = entity.ExternalToken,
                ExternalWorkerId = entity.ExternalWorkerId,
                ExternalTokenExpiry = entity.ExternalTokenExpiry,
                SubscriptionData = JsonConvert.DeserializeObject(entity.SubscriptionData, SerializerSettings),
            };
        }
    }
}