using Amazon.DynamoDBv2.Model;
using Amazon.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
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
            result["description"] = new AttributeValue(source.Description);
            result["reference"] = new AttributeValue(source.Reference);
            result["workflowDefinitionId"] = new AttributeValue(source.WorkflowDefinitionId);
            result["version"] = new AttributeValue(source.Version.ToString());
            result["nextExecution"] = new AttributeValue() { N = (source.NextExecution ?? 0).ToString() };
            result["createTime"] = new AttributeValue() { N = source.CreateTime.Ticks.ToString() };
            result["data"] = new AttributeValue(JsonConvert.SerializeObject(source.Data, SerializerSettings));
            result["status"] = new AttributeValue() { N = Convert.ToInt32(source.Status).ToString() };

            if (source.CompleteTime.HasValue)
                result["completeTime"] = new AttributeValue() { N = source.CompleteTime.Value.Ticks.ToString() };

            var pointers = new List<AttributeValue>();
            foreach (var pointer in source.ExecutionPointers)
            {
                pointers.Add(new AttributeValue()
                {
                    M = pointer.ToDynamoMap()
                });
            }

            result["executionPointers"] = new AttributeValue() { L = pointers };

            return result;
        }

        public static WorkflowInstance ToWorkflowInstance(this Dictionary<string, AttributeValue> source)
        {
            var result = new WorkflowInstance()
            {
                Id = source["id"].S,
                Description = source["description"].S,
                Reference = source["reference"].S,
                WorkflowDefinitionId = source["workflowDefinitionId"].S,
                Version = Convert.ToInt32(source["version"].S),
                Status = (WorkflowStatus)Convert.ToInt32(source["status"].N),
                NextExecution = Convert.ToInt64(source["nextExecution"].N),
                CreateTime = new DateTime(Convert.ToInt64(source["createTime"].N)),
                Data = JsonConvert.DeserializeObject(source["data"].S, SerializerSettings)
            };

            if (source["completeTime"] != null)
                result.CompleteTime = new DateTime(Int64.Parse(source["completeTime"].N));
            
            foreach (var pointer in source["executionPointers"].L)
            {
                result.ExecutionPointers.Add(pointer.M.ToExecutionPointer());
            }

            return result;
        }

        private static Dictionary<string, AttributeValue> ToDynamoMap(this ExecutionPointer source)
        {
            var result = new Dictionary<string, AttributeValue>();
            result["id"] = new AttributeValue(source.Id);
            result["active"] = new AttributeValue() { BOOL = source.Active };
            result["eventPublished"] = new AttributeValue() { BOOL = source.EventPublished };
            result["eventKey"] = new AttributeValue(source.EventKey);
            result["eventName"] = new AttributeValue(source.EventName);
            result["predecessorId"] = new AttributeValue(source.PredecessorId);
            result["stepName"] = new AttributeValue(source.StepName);
            result["children"] = new AttributeValue(source.Children);
            result["scope"] = new AttributeValue(source.Scope.ToList());
            result["contextItem"] = new AttributeValue(JsonConvert.SerializeObject(source.ContextItem, SerializerSettings));
            result["eventData"] = new AttributeValue(JsonConvert.SerializeObject(source.EventData, SerializerSettings));
            result["persistenceData"] = new AttributeValue(JsonConvert.SerializeObject(source.PersistenceData, SerializerSettings));
            result["outcome"] = new AttributeValue(JsonConvert.SerializeObject(source.Outcome, SerializerSettings));
            result["stepId"] = new AttributeValue() { N = source.StepId.ToString() };
            result["retryCount"] = new AttributeValue() { N = source.RetryCount.ToString() };
            result["status"] = new AttributeValue() { N = Convert.ToInt32(source.Status).ToString() };

            if (source.SleepUntil.HasValue)
                result["sleepUntil"] = new AttributeValue() { N = source.SleepUntil.Value.Ticks.ToString() };
            
            if (source.StartTime.HasValue)
                result["startTime"] = new AttributeValue() { N = source.StartTime.Value.Ticks.ToString() };

            if (source.EndTime.HasValue)
                result["endTime"] = new AttributeValue() { N = source.EndTime.Value.Ticks.ToString() };

            result["extensionAttributes"] = new AttributeValue(JsonConvert.SerializeObject(source.ExtensionAttributes, SerializerSettings));

            return result;
        }

        public static ExecutionPointer ToExecutionPointer(this Dictionary<string, AttributeValue> source)
        {
            var result = new ExecutionPointer
            {
                Id = source["id"].S,
                Active = source["active"].BOOL,
                ContextItem = JsonConvert.DeserializeObject(source["contextItem"].S, SerializerSettings),
                Children = source["children"].SS,
                StepId = Convert.ToInt32(source["stepId"].N),
                RetryCount = Convert.ToInt32(source["retryCount"].N),
                EventKey = source["eventKey"].S,
                EventName = source["eventName"].S,
                EventPublished = source["eventPublished"].BOOL,
                PredecessorId = source["predecessorId"].S,
                Scope = new Stack<string>(source["scope"].SS),
                StepName = source["stepName"].S,
                Status = (PointerStatus)(Convert.ToInt32(source["status"].N)),
                PersistenceData = JsonConvert.DeserializeObject(source["persistenceData"].S, SerializerSettings),
                EventData = JsonConvert.DeserializeObject(source["eventData"].S, SerializerSettings),
                Outcome = JsonConvert.DeserializeObject(source["outcome"].S, SerializerSettings),

                ExtensionAttributes = JsonConvert.DeserializeObject<Dictionary<string, object>>(source["extensionAttributes"].S, SerializerSettings)
            };
            
            if (source["startTime"] != null)
                result.StartTime = new DateTime(Int64.Parse(source["startTime"].N));

            if (source["endTime"] != null)
                result.EndTime = new DateTime(Int64.Parse(source["endTime"].N));

            if (source["sleepUntil"] != null)
                result.SleepUntil = new DateTime(Int64.Parse(source["sleepUntil"].N));

            return result;
        }
    }
}