using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Models;

namespace WorkflowCore.Persistence.EntityFramework
{
    internal static class ExtensionMethods
    {
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        internal static PersistedWorkflow ToPersistable(this WorkflowInstance instance)
        {
            PersistedWorkflow result = new PersistedWorkflow();            
            result.Data = JsonConvert.SerializeObject(instance.Data, SerializerSettings);
            result.Description = instance.Description;
            result.InstanceId = new Guid(instance.Id);
            result.NextExecution = instance.NextExecution;
            result.Version = instance.Version;
            result.WorkflowDefinitionId = instance.WorkflowDefinitionId;
            result.ExecutionPointers = JsonConvert.SerializeObject(instance.ExecutionPointers, SerializerSettings);

            return result;
        }

        internal static PersistedSubscription ToPersistable(this EventSubscription instance)
        {
            PersistedSubscription result = new PersistedSubscription();            
            result.SubscriptionId = new Guid(instance.Id);
            result.EventKey = instance.EventKey;
            result.EventName = instance.EventName;
            result.StepId = instance.StepId;
            result.WorkflowId = instance.WorkflowId;

            return result;
        }

        internal static PersistedPublication ToPersistable(this EventPublication instance)
        {
            PersistedPublication result = new PersistedPublication();
            result.PublicationId = instance.Id;
            result.EventKey = instance.EventKey;
            result.EventName = instance.EventName;
            result.StepId = instance.StepId;
            result.WorkflowId = instance.WorkflowId;
            result.EventData = JsonConvert.SerializeObject(instance.EventData, SerializerSettings);

            return result;
        }

        internal static WorkflowInstance ToWorkflowInstance(this PersistedWorkflow instance)
        {
            WorkflowInstance result = new WorkflowInstance();
            result.Data = JsonConvert.DeserializeObject(instance.Data, SerializerSettings);
            result.Description = instance.Description;
            result.Id = instance.InstanceId.ToString();
            result.NextExecution = instance.NextExecution;
            result.Version = instance.Version;
            result.WorkflowDefinitionId = instance.WorkflowDefinitionId;
            result.ExecutionPointers = JsonConvert.DeserializeObject<List<ExecutionPointer>>(instance.ExecutionPointers, SerializerSettings);

            return result;
        }

        internal static EventSubscription ToEventSubscription(this PersistedSubscription instance)
        {
            EventSubscription result = new EventSubscription();
            result.Id = instance.SubscriptionId.ToString();
            result.EventKey = instance.EventKey;
            result.EventName = instance.EventName;
            result.StepId = instance.StepId;
            result.WorkflowId = instance.WorkflowId;

            return result;
        }

        internal static EventPublication ToEventPublication(this PersistedPublication instance)
        {
            EventPublication result = new EventPublication();
            result.Id = instance.PublicationId;
            result.EventKey = instance.EventKey;
            result.EventName = instance.EventName;
            result.StepId = instance.StepId;
            result.WorkflowId = instance.WorkflowId;
            result.EventData = JsonConvert.DeserializeObject(instance.EventData, SerializerSettings);

            return result;
        }
    }
}
