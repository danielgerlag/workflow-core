using System;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.Azure.Models
{
    public class WorkflowTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string WorkflowDefinitionId { get; set; }
        public int Version { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public string ExecutionPointers { get; set; }
        public long? NextExecution { get; set; }
        public int Status { get; set; }
        public string Data { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? CompleteTime { get; set; }

        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public static WorkflowTableEntity FromInstance(WorkflowInstance instance)
        {
            return new WorkflowTableEntity
            {
                PartitionKey = "workflow",
                RowKey = instance.Id,
                WorkflowDefinitionId = instance.WorkflowDefinitionId,
                Version = instance.Version,
                Description = instance.Description,
                Reference = instance.Reference,
                NextExecution = instance.NextExecution,
                Status = (int)instance.Status,
                CreateTime = instance.CreateTime,
                CompleteTime = instance.CompleteTime,
                Data = JsonConvert.SerializeObject(instance.Data, SerializerSettings),
                ExecutionPointers = JsonConvert.SerializeObject(instance.ExecutionPointers, SerializerSettings),
            };
        }

        public static WorkflowInstance ToInstance(WorkflowTableEntity entity)
        {
            return new WorkflowInstance
            {
                Id = entity.RowKey,
                WorkflowDefinitionId = entity.WorkflowDefinitionId,
                Version = entity.Version,
                Description = entity.Description,
                Reference = entity.Reference,
                NextExecution = entity.NextExecution,
                Status = (WorkflowStatus)entity.Status,
                CreateTime = entity.CreateTime,
                CompleteTime = entity.CompleteTime,
                Data = JsonConvert.DeserializeObject(entity.Data, SerializerSettings),
                ExecutionPointers = JsonConvert.DeserializeObject<ExecutionPointerCollection>(entity.ExecutionPointers, SerializerSettings),
            };
        }
    }
}