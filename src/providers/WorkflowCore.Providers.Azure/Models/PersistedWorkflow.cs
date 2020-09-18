using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.Azure.Models
{
    public class PersistedWorkflow
    {
        public string id { get; set; }

        public string WorkflowDefinitionId { get; set; }

        public int Version { get; set; }

        public string Description { get; set; }

        public string Reference { get; set; }

        public string ExecutionPointers { get; set; }

        public long? NextExecution { get; set; }

        public WorkflowStatus Status { get; set; }

        public string Data { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? CompleteTime { get; set; }

        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        public static PersistedWorkflow FromInstance(WorkflowInstance instance)
        {
            var result = new PersistedWorkflow()
            {
                id = instance.Id,
                CompleteTime = instance.CompleteTime,
                CreateTime = instance.CreateTime,
                Description = instance.Description,
                NextExecution = instance.NextExecution,
                Reference = instance.Reference,
                Status = instance.Status,
                Version = instance.Version,
                WorkflowDefinitionId = instance.WorkflowDefinitionId,
                Data = JsonConvert.SerializeObject(instance.Data, SerializerSettings),
                ExecutionPointers = JsonConvert.SerializeObject(instance.ExecutionPointers, SerializerSettings),
            };

            return result;
        }

        public static WorkflowInstance ToInstance(PersistedWorkflow instance)
        {
            var result = new WorkflowInstance()
            {
                Id = instance.id,
                CompleteTime = instance.CompleteTime,
                CreateTime = instance.CreateTime,
                Description = instance.Description,
                NextExecution = instance.NextExecution,
                Reference = instance.Reference,
                Status = instance.Status,
                Version = instance.Version,
                WorkflowDefinitionId = instance.WorkflowDefinitionId,
                Data = JsonConvert.DeserializeObject(instance.Data, SerializerSettings),
                ExecutionPointers = JsonConvert.DeserializeObject<ExecutionPointerCollection>(instance.ExecutionPointers, SerializerSettings),
            };

            return result;
        }

    }
}
