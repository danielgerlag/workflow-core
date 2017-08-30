using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowHost
    {
        /// <summary>
        /// Start the workflow host, this enable execution of workflows
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the workflow host
        /// </summary>
        void Stop();
        Task<string> StartWorkflow(string workflowId, object data = null);
        Task<string> StartWorkflow(string workflowId, int? version, object data = null);
        Task<string> StartWorkflow<TData>(string workflowId, TData data = null) where TData : class;
        Task<string> StartWorkflow<TData>(string workflowId, int? version, TData data = null) where TData : class;
                        
        Task PublishEvent(string eventName, string eventKey, object eventData, DateTime? effectiveDate = null);
        void RegisterWorkflow<TWorkflow>() where TWorkflow : IWorkflow, new();
        void RegisterWorkflow<TWorkflow, TData>() where TWorkflow : IWorkflow<TData>, new() where TData : new();

        /// <summary>
        /// Suspend the execution of a given workflow until .ResumeWorkflow is called
        /// </summary>
        /// <param name="workflowId"></param>
        /// <returns></returns>
        Task<bool> SuspendWorkflow(string workflowId);

        /// <summary>
        /// Resume a previously suspended workflow
        /// </summary>
        /// <param name="workflowId"></param>
        /// <returns></returns>
        Task<bool> ResumeWorkflow(string workflowId);

        /// <summary>
        /// Permanently terminate the exeuction of a given workflow
        /// </summary>
        /// <param name="workflowId"></param>
        /// <returns></returns>
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
