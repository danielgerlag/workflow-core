using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using System.Reflection;
using WorkflowCore.Exceptions;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services
{
    public class WorkflowHost : WorkflowController, IWorkflowHost, IDisposable
    {
        protected bool _shutdown = true;
        protected readonly IServiceProvider _serviceProvider;

        private readonly IEnumerable<IBackgroundTask> _backgroundTasks;

        public event StepErrorEventHandler OnStepError;
        public event LifeCycleEventHandler OnLifeCycleEvent;

        // Public dependencies to allow for extension method access.
        public IPersistenceProvider PersistenceStore { get; private set; }
        public IDistributedLockProvider LockProvider { get; private set; }
        public IWorkflowRegistry Registry { get; private set; }
        public WorkflowOptions Options { get; private set; }
        public IQueueProvider QueueProvider { get; private set; }
        public ILogger Logger { get; private set; }

        public WorkflowHost(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, WorkflowOptions options, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, IEnumerable<IBackgroundTask> backgroundTasks, IExecutionPointerFactory pointerFactory, ILifeCycleEventHub lifeCycleEventHub)
            : base(persistenceStore, lockProvider, registry, queueProvider, pointerFactory, lifeCycleEventHub, loggerFactory)
        {
            PersistenceStore = persistenceStore;
            QueueProvider = queueProvider;
            Options = options;
            Logger = loggerFactory.CreateLogger<WorkflowHost>();
            _serviceProvider = serviceProvider;
            Registry = registry;
            LockProvider = lockProvider;
            _backgroundTasks = backgroundTasks;
            persistenceStore.EnsureStoreExists();
            lifeCycleEventHub.Subscribe(HandleLifeCycleEvent);
        }

        public void Start()
        {
            _shutdown = false;
            PersistenceStore.EnsureStoreExists();
            QueueProvider.Start().Wait();
            LockProvider.Start().Wait();

            Logger.LogInformation("Starting backgroud tasks");

            foreach (var task in _backgroundTasks)
                task.Start();
        }

        public void Stop()
        {
            _shutdown = true;

            Logger.LogInformation("Stopping background tasks");
            foreach (var th in _backgroundTasks)
                th.Stop();

            Logger.LogInformation("Worker tasks stopped");

            QueueProvider.Stop();
            LockProvider.Stop();
        }

        public void RegisterWorkflow<TWorkflow>()
            where TWorkflow : IWorkflow, new()
        {
            TWorkflow wf = new TWorkflow();
            Registry.RegisterWorkflow(wf);
        }

        public void RegisterWorkflow<TWorkflow, TData>()
            where TWorkflow : IWorkflow<TData>, new()
            where TData : new()
        {
            TWorkflow wf = new TWorkflow();
            Registry.RegisterWorkflow<TData>(wf);
        }

        public void HandleLifeCycleEvent(LifeCycleEvent evt)
        {
            OnLifeCycleEvent?.Invoke(evt);
        }

        public void ReportStepError(WorkflowInstance workflow, WorkflowStep step, Exception exception)
        {
            OnStepError?.Invoke(workflow, step, exception);
        }

        public void Dispose()
        {
            if (!_shutdown)
                Stop();
        }
    }
}
