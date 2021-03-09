﻿using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowExecutor
    {
        Task<WorkflowExecutorResult> Execute(WorkflowInstance workflow, CancellationToken cancellationToken = default);
        Task<WorkflowExecutorResult> Execute(WorkflowInstance workflow, WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
    }
}