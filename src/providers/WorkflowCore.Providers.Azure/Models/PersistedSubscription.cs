using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.Azure.Models
{
    public class PersistedSubscription
    {
        public string id { get; set; }

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

        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        public static PersistedSubscription FromInstance(EventSubscription instance)
        {
            return new PersistedSubscription()
            {
                id = instance.Id,
                EventKey = instance.EventKey,
                EventName = instance.EventName,
                ExecutionPointerId = instance.ExecutionPointerId,
                ExternalToken = instance.ExternalToken,
                ExternalTokenExpiry = instance.ExternalTokenExpiry,
                ExternalWorkerId = instance.ExternalWorkerId,
                StepId = instance.StepId,
                SubscribeAsOf = instance.SubscribeAsOf,
                WorkflowId = instance.WorkflowId,
                SubscriptionData = JsonConvert.SerializeObject(instance.SubscriptionData, SerializerSettings),
            };
        }

        public static EventSubscription ToInstance(PersistedSubscription instance)
        {
            return new EventSubscription()
            {
                Id = instance.id,
                EventKey = instance.EventKey,
                EventName = instance.EventName,
                ExecutionPointerId = instance.ExecutionPointerId,
                ExternalToken = instance.ExternalToken,
                ExternalTokenExpiry = instance.ExternalTokenExpiry,
                ExternalWorkerId = instance.ExternalWorkerId,
                StepId = instance.StepId,
                SubscribeAsOf = instance.SubscribeAsOf,
                WorkflowId = instance.WorkflowId,
                SubscriptionData = JsonConvert.DeserializeObject(instance.SubscriptionData, SerializerSettings),
            };
        }
    }
}
