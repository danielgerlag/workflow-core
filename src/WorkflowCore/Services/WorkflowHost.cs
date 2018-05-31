﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using System.Reflection;
using WorkflowCore.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace WorkflowCore.Services
{
    public class WorkflowHost : IWorkflowHost, IDisposable
    {                
        protected bool _shutdown = true;        
        protected readonly IServiceProvider _serviceProvider;

        private readonly IEnumerable<IBackgroundTask> _backgroundTasks;
        private readonly IWorkflowController _workflowController;

        public event StepErrorEventHandler OnStepError;

        // Public dependencies to allow for extension method access.
        public IPersistenceProvider PersistenceStore { get; private set; }
        public IDistributedLockProvider LockProvider { get; private set; }
        public IWorkflowRegistry Registry { get; private set; }
        public WorkflowOptions Options { get; private set; }
        public IQueueProvider QueueProvider { get; private set; }
        public ILogger Logger { get; private set; }

        public WorkflowHost(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, WorkflowOptions options, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider, IEnumerable<IBackgroundTask> backgroundTasks, IWorkflowController workflowController)
        {
            PersistenceStore = persistenceStore;
            QueueProvider = queueProvider;
            Options = options;
            Logger = loggerFactory.CreateLogger<WorkflowHost>();
            _serviceProvider = serviceProvider;
            Registry = registry;
            LockProvider = lockProvider;
            _backgroundTasks = backgroundTasks;
            _workflowController = workflowController;
            persistenceStore.EnsureStoreExists();
        }

        public Task<string> StartWorkflow(string workflowId, object data = null)
        {
            return _workflowController.StartWorkflow(workflowId, data);
        }

        public Task<string> StartWorkflow(string workflowId, int? version, object data = null)
        {
            return _workflowController.StartWorkflow<object>(workflowId, version, data);
        }

        public Task<string> StartWorkflow<TData>(string workflowId, TData data = null)
            where TData : class
        {
            return _workflowController.StartWorkflow<TData>(workflowId, null, data);
        }


        public Task<string> StartWorkflow<TData>(string workflowId, int? version, TData data = null)
            where TData : class
        {
            return _workflowController.StartWorkflow(workflowId, version, data);
        }

        public Task PublishEvent(string eventName, string eventKey, object eventData, DateTime? effectiveDate = null)
        {
            return _workflowController.PublishEvent(eventName, eventKey, eventData, effectiveDate);
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

        public void RegisterWorkflow(IWorkflow workflow)
        {
            Registry.RegisterWorkflow(workflow);
        }

        public void RegisterWorkflow<TData>(IWorkflow<TData> workflow) where TData : new()
        {
            Registry.RegisterWorkflow(workflow);
        }

        public void RegisterWorkflow<TWorkflow>() 
            where TWorkflow : IWorkflow, new()
        {
            var wfs = _serviceProvider.GetServices<IWorkflow>();
            var wf = wfs.FirstOrDefault(w => typeof(TWorkflow).FullName == w.GetType().FullName) ?? new TWorkflow();
            Registry.RegisterWorkflow(wf);
        }

        public void RegisterWorkflow<TWorkflow, TData>() 
            where TWorkflow : IWorkflow<TData>, new()
            where TData : new()
        {
            var wfs = _serviceProvider.GetServices<IWorkflow<TData>>();
            var wf = wfs.FirstOrDefault(w => typeof(TWorkflow).FullName == w.GetType().FullName) ?? new TWorkflow();
            Registry.RegisterWorkflow(wf);
        }

        public Task<bool> SuspendWorkflow(string workflowId)
        {
            return _workflowController.SuspendWorkflow(workflowId);
        }

        public Task<bool> ResumeWorkflow(string workflowId)
        {
            return _workflowController.ResumeWorkflow(workflowId);
        }

        public Task<bool> TerminateWorkflow(string workflowId)
        {
            return _workflowController.TerminateWorkflow(workflowId);
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
