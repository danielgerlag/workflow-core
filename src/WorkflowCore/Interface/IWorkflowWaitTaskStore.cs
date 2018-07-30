using System;
using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    public interface IWorkflowWaitTaskStore
    {
        void AddTask(string workflowId);
        Task RemoveTask(string workflowId);
        Task Wait(string workflowId);
        Task Wait(string workflowId, TimeSpan timeout);
    }
}