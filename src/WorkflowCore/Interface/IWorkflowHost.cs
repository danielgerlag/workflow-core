using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowHost
    {
        void Start();
        void Stop();
        Task<string> StartWorkflow(string workflowId, int version, object data);
        Task<string> StartWorkflow<TData>(string workflowId, int version, TData data);
        Task SubscribeEvent(string workflowId, int stepId, string eventName, string eventKey);
        Task PublishEvent(string eventName, string eventKey, object eventData);
        void RegisterWorkflow<TWorkflow>() where TWorkflow : IWorkflow, new();
        void RegisterWorkflow<TWorkflow, TData>() where TWorkflow : IWorkflow<TData>, new() where TData : new();

        Task<bool> SuspendWorkflow(string workflowId);
        Task<bool> ResumeWorkflow(string workflowId);
        Task<bool> TerminateWorkflow(string workflowId);

    }
}
