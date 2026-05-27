using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Hosting;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Interface
{
    public interface IWorkflowHost : IWorkflowController, IActivityController, IHostedService
    {
        /// <summary>
        /// Start the workflow host, this enable execution of workflows
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the workflow host
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Fires events when an error is thrown in a workflow step.
        /// </summary>
        event StepErrorEventHandler OnStepError;

        /// <summary>
        /// Fires events on workflow and step lifecycle changes like started and completed steps/workflows
        /// and workflow errors.
        /// </summary>
        event LifeCycleEventHandler OnLifeCycleEvent;
        void ReportStepError(WorkflowInstance workflow, WorkflowStep step, Exception exception);

        //public dependencies to allow for extension method access

        /// <summary>
        /// Persists workflow data like workflow status and events.
        /// </summary>
        IPersistenceProvider PersistenceStore { get; }

        /// <summary>
        /// Provides locks for resources in the persistence store to ensure exclusive access.
        /// </summary>
        IDistributedLockProvider LockProvider { get; }

        /// <summary>
        /// Contains the workflow definitions which are used for creating workflow instances.
        /// </summary>
        IWorkflowRegistry Registry { get; }
        WorkflowOptions Options { get; }
        IQueueProvider QueueProvider { get; }
        ILogger Logger { get; }

    }

    public delegate void StepErrorEventHandler(WorkflowInstance workflow, WorkflowStep step, Exception exception);
    public delegate void LifeCycleEventHandler(LifeCycleEvent evt);
}