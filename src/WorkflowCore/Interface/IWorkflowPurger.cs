using System;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowPurger
    {
        Task PurgeWorkflows(WorkflowStatus status, DateTime olderThan);
    }
}