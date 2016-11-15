using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowRuntime
    {
        void StartRuntime();
        void StopRuntime();
        Task<string> StartWorkflow(string workflowId, int version, object data);
        Task<string> StartWorkflow<TData>(string workflowId, int version, TData data);
        Task SubscribeEvent(string workflowId, int stepId, string eventName, string eventKey);
        Task PublishEvent(string eventName, string eventKey, object eventData);

    }
}
