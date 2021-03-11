using System;
using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    public interface IWorkflowController
    {
        Task<string> StartWorkflow(string workflowId, object data = null, string reference=null);
        Task<string> StartWorkflow(string workflowId, int? version, object data = null, string reference=null);
        Task<string> StartWorkflow<TData>(string workflowId, TData data = null, string reference=null) where TData : class, new();
        Task<string> StartWorkflow<TData>(string workflowId, int? version, TData data = null, string reference=null) where TData : class, new();

        Task PublishEvent(string eventName, string eventKey, object eventData, DateTime? effectiveDate = null);
        void RegisterWorkflow<TWorkflow>() where TWorkflow : IWorkflow;
        void RegisterWorkflow<TWorkflow, TData>() where TWorkflow : IWorkflow<TData> where TData : new();

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

    }
}
