using Microsoft.Extensions.Logging;
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
        Task<string> StartWorkflow(string workflowId, object data = null);
        Task<string> StartWorkflow(string workflowId, int? version, object data = null);
        Task<string> StartWorkflow<TData>(string workflowId, TData data = null) where TData : class;
        Task<string> StartWorkflow<TData>(string workflowId, int? version, TData data = null) where TData : class;


        Task SubscribeEvent(string workflowId, int stepId, string eventName, string eventKey, DateTime asOf);
        Task PublishEvent(string eventName, string eventKey, object eventData);
        void RegisterWorkflow<TWorkflow>() where TWorkflow : IWorkflow, new();
        void RegisterWorkflow<TWorkflow, TData>() where TWorkflow : IWorkflow<TData>, new() where TData : new();

        Task<bool> SuspendWorkflow(string workflowId);
        Task<bool> ResumeWorkflow(string workflowId);
        Task<bool> TerminateWorkflow(string workflowId);

        event StepErrorEventHandler OnStepError;
        void ReportStepError(WorkflowInstance workflow, WorkflowStep step, Exception exception);

        //public dependencies to allow for extension method access
        IPersistenceProvider PersistenceStore { get; }
        IDistributedLockProvider LockProvider { get; }
        IWorkflowRegistry Registry { get; }
        WorkflowOptions Options { get; }
        IQueueProvider QueueProvider { get; }
        ILogger Logger { get; }

    }

    public delegate void StepErrorEventHandler(WorkflowInstance workflow, WorkflowStep step, Exception exception);
}
