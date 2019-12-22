using Amazon.DynamoDBv2.Model;
using Amazon.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.AWS
{
    internal static class ModelExtensions
    {
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        public static Dictionary<string, AttributeValue> ToDynamoMap(this WorkflowInstance source)
        {
            var result = new Dictionary<string, AttributeValue>();

            result["id"] = new AttributeValue(source.Id);
            result["workflow_definition_id"] = new AttributeValue(source.WorkflowDefinitionId);
            result["version"] = new AttributeValue(source.Version.ToString());
            result["next_execution"] = new AttributeValue() { N = (source.NextExecution ?? 0).ToString() };
            result["create_time"] = new AttributeValue() { N = source.CreateTime.Ticks.ToString() };
            result["data"] = new AttributeValue(JsonConvert.SerializeObject(source.Data, SerializerSettings));
            result["workflow_status"] = new AttributeValue() { N = Convert.ToInt32(source.Status).ToString() };

            if (!string.IsNullOrEmpty(source.Description))
                result["description"] = new AttributeValue(source.Description);

            if (!string.IsNullOrEmpty(source.Reference))
                result["reference"] = new AttributeValue(source.Reference);

            if (source.CompleteTime.HasValue)
                result["complete_time"] = new AttributeValue() { N = source.CompleteTime.Value.Ticks.ToString() };
            
            var pointers = new List<AttributeValue>();
            foreach (var pointer in source.ExecutionPointers)
            {
                pointers.Add(new AttributeValue(JsonConvert.SerializeObject(pointer, SerializerSettings)));
            }

            result["pointers"] = new AttributeValue() { L = pointers };

            if (source.Status == WorkflowStatus.Runnable)
                result["runnable"] = new AttributeValue() { N = 1.ToString() };

            return result;
        }

        public static WorkflowInstance ToWorkflowInstance(this Dictionary<string, AttributeValue> source)
        {
            var result = new WorkflowInstance()
            {
                Id = source["id"].S,
                WorkflowDefinitionId = source["workflow_definition_id"].S,
                Version = Convert.ToInt32(source["version"].S),
                Status = (WorkflowStatus)Convert.ToInt32(source["workflow_status"].N),
                NextExecution = Convert.ToInt64(source["next_execution"].N),
                CreateTime = new DateTime(Convert.ToInt64(source["create_time"].N)),
                Data = JsonConvert.DeserializeObject(source["data"].S, SerializerSettings)
            };

            if (source.ContainsKey("description"))
                result.Description = source["description"].S;

            if (source.ContainsKey("reference"))
                result.Reference = source["reference"].S;

            if (source.ContainsKey("complete_time"))
                result.CompleteTime = new DateTime(Int64.Parse(source["complete_time"].N));
            
            foreach (var pointer in source["pointers"].L)
            {
                var ep = JsonConvert.DeserializeObject<ExecutionPointer>(pointer.S, SerializerSettings);
                result.ExecutionPointers.Add(ep);
            }

            return result;
        }

        public static Dictionary<string, AttributeValue> ToDynamoMap(this EventSubscription source)
        {
            var result =  new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue(source.Id),
                ["event_name"] = new AttributeValue(source.EventName),
                ["event_key"] = new AttributeValue(source.EventKey),
                ["workflow_id"] = new AttributeValue(source.WorkflowId),
                ["execution_pointer_id"] = new AttributeValue(source.ExecutionPointerId),
                ["step_id"] = new AttributeValue(source.StepId.ToString()),
                ["subscribe_as_of"] = new AttributeValue() { N = source.SubscribeAsOf.Ticks.ToString() },
                ["subscription_data"] = new AttributeValue(JsonConvert.SerializeObject(source.SubscriptionData, SerializerSettings)),
                ["event_slug"] = new AttributeValue($"{source.EventName}:{source.EventKey}")
            };
            if (!string.IsNullOrEmpty(source.ExternalToken))
                result["external_token"] = new AttributeValue(source.ExternalToken);
                    
            if (!string.IsNullOrEmpty(source.ExternalWorkerId))
                result["external_worker_id"] = new AttributeValue(source.ExternalWorkerId);

            if (source.ExternalTokenExpiry.HasValue)
                result["external_token_expiry"] = new AttributeValue() { N = source.ExternalTokenExpiry.Value.Ticks.ToString()};

            return result;
        }

        public static EventSubscription ToEventSubscription(this Dictionary<string, AttributeValue> source)
        {
            var result =  new EventSubscription()
            {
                Id = source["id"].S,
                EventName = source["event_name"].S,
                EventKey = source["event_key"].S,
                WorkflowId = source["workflow_id"].S,
                ExecutionPointerId = source["execution_pointer_id"].S,
                StepId = Convert.ToInt32(source["step_id"].S),
                SubscribeAsOf = new DateTime(Convert.ToInt64(source["subscribe_as_of"].N)),
                SubscriptionData = JsonConvert.DeserializeObject(source["subscription_data"].S, SerializerSettings),
            };
            
            if (source.ContainsKey("external_token"))
                result.ExternalToken = source["external_token"].S;
            
            if (source.ContainsKey("external_worker_id"))
                result.ExternalWorkerId = source["external_worker_id"].S;
            
            if (source.ContainsKey("external_token_expiry"))
                result.ExternalTokenExpiry = new DateTime(Int64.Parse(source["external_token_expiry"].N));
            
            return result;
        }

        public static Dictionary<string, AttributeValue> ToDynamoMap(this Event source)
        {
            var result = new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue(source.Id),
                ["event_name"] = new AttributeValue(source.EventName),
                ["event_key"] = new AttributeValue(source.EventKey),
                ["event_data"] = new AttributeValue(JsonConvert.SerializeObject(source.EventData, SerializerSettings)),
                ["event_time"] = new AttributeValue() { N = source.EventTime.Ticks.ToString() },
                ["event_slug"] = new AttributeValue($"{source.EventName}:{source.EventKey}")
            };

            if (!source.IsProcessed)
                result["not_processed"] = new AttributeValue() { N = 1.ToString() };

            return result;
        }

        public static Event ToEvent(this Dictionary<string, AttributeValue> source)
        {
            var result = new Event()
            {
                Id = source["id"].S,
                EventName = source["event_name"].S,
                EventKey = source["event_key"].S,
                EventData = JsonConvert.DeserializeObject(source["event_data"].S, SerializerSettings),
                EventTime = new DateTime(Convert.ToInt64(source["event_time"].N)),
                IsProcessed = (!source.ContainsKey("not_processed"))
            };

            return result;
        }
    }
}