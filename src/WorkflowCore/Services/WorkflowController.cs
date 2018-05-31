﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Exceptions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowController : IWorkflowController
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IWorkflowRegistry _registry;
        private readonly IQueueProvider _queueProvider;
        private readonly IExecutionPointerFactory _pointerFactory;
        private readonly ILogger _logger;
        
        public WorkflowController(IPersistenceProvider persistenceStore, IDistributedLockProvider lockProvider, IWorkflowRegistry registry, IQueueProvider queueProvider, IExecutionPointerFactory pointerFactory, ILoggerFactory loggerFactory)
        {
            _persistenceStore = persistenceStore;
            _lockProvider = lockProvider;
            _registry = registry;
            _queueProvider = queueProvider;
            _pointerFactory = pointerFactory;
            _logger = loggerFactory.CreateLogger<WorkflowController>();
        }

        public Task<string> StartWorkflow(string workflowId, object data = null)
        {
            return StartWorkflow(workflowId, null, data);
        }

        public Task<string> StartWorkflow(string workflowId, int? version, object data = null)
        {
            return StartWorkflow<object>(workflowId, version, data);
        }

        public Task<string> StartWorkflow<TData>(string workflowId, TData data = null) 
            where TData : class
        {
            return StartWorkflow<TData>(workflowId, null, data);
        }

        public async Task<string> StartWorkflow<TData>(string workflowId, int? version, TData data = null)
            where TData : class
        {
            
            var def = _registry.GetDefinition(workflowId, version);
            if (def == null)
            {
                throw new WorkflowNotRegisteredException(workflowId, version);
            }

            var wf = new WorkflowInstance
            {
                WorkflowDefinitionId = workflowId,
                Version = def.Version,
                Data = data,
                Description = def.Description,
                NextExecution = 0,
                CreateTime = DateTime.Now.ToUniversalTime(),
                Status = WorkflowStatus.Runnable
            };

            if ((def.DataType != null) && (data == null))
            {
                wf.Data = TypeExtensions.GetConstructor(def.DataType, new Type[] { }).Invoke(null);
            }

            wf.ExecutionPointers.Add(_pointerFactory.BuildGenesisPointer(def));

            string id = await _persistenceStore.CreateNewWorkflow(wf);
            await _queueProvider.QueueWork(id, QueueType.Workflow);
            return id;
        }

        public async Task PublishEvent(string eventName, string eventKey, object eventData, DateTime? effectiveDate = null)
        {
            _logger.LogDebug("Creating event {0} {1}", eventName, eventKey);
            Event evt = new Event();

            if (effectiveDate.HasValue)
                evt.EventTime = effectiveDate.Value.ToUniversalTime();
            else
                evt.EventTime = DateTime.Now.ToUniversalTime();

            evt.EventData = eventData;
            evt.EventKey = eventKey;
            evt.EventName = eventName;
            evt.IsProcessed = false;
            string eventId = await _persistenceStore.CreateEvent(evt);

            await _queueProvider.QueueWork(eventId, QueueType.Event);
        }

        public async Task<bool> SuspendWorkflow(string workflowId)
        {
            if (!await _lockProvider.AcquireLock(workflowId, new CancellationToken()))
                return false;
            
            try
            {
                var wf = await _persistenceStore.GetWorkflowInstance(workflowId);
                if (wf.Status == WorkflowStatus.Runnable)
                {
                    wf.Status = WorkflowStatus.Suspended;
                    await _persistenceStore.PersistWorkflow(wf);
                    return true;
                }

                return false;
            }
            finally
            {
                await _lockProvider.ReleaseLock(workflowId);
            }
        }

        public async Task<bool> ResumeWorkflow(string workflowId)
        {
            if (!await _lockProvider.AcquireLock(workflowId, new CancellationToken()))
            {
                return false;
            }

            bool requeue = false;
            try
            {
                var wf = await _persistenceStore.GetWorkflowInstance(workflowId);
                if (wf.Status == WorkflowStatus.Suspended)
                {
                    wf.Status = WorkflowStatus.Runnable;
                    await _persistenceStore.PersistWorkflow(wf);
                    requeue = true;
                    return true;
                }

                return false;
            }
            finally
            {
                await _lockProvider.ReleaseLock(workflowId);
                if (requeue)
                    await _queueProvider.QueueWork(workflowId, QueueType.Workflow);
            }
        }

        public async Task<bool> TerminateWorkflow(string workflowId)
        {
            if (!await _lockProvider.AcquireLock(workflowId, new CancellationToken()))
            {
                return false;
            }

            try
            {
                var wf = await _persistenceStore.GetWorkflowInstance(workflowId);
                wf.Status = WorkflowStatus.Terminated;
                await _persistenceStore.PersistWorkflow(wf);
                return true;
            }
            finally
            {
                await _lockProvider.ReleaseLock(workflowId);
            }
        }
        
        public void RegisterWorkflow<TWorkflow>()
            where TWorkflow : IWorkflow, new()
        {
            TWorkflow wf = new TWorkflow();
            _registry.RegisterWorkflow(wf);
        }

        public void RegisterWorkflow<TWorkflow, TData>()
            where TWorkflow : IWorkflow<TData>, new()
            where TData : new()
        {
            TWorkflow wf = new TWorkflow();
            _registry.RegisterWorkflow<TData>(wf);
        }

        public void RegisterWorkflow(IWorkflow workflow)
        {
            _registry.RegisterWorkflow(workflow);
        }

        public void RegisterWorkflow<TData>(IWorkflow<TData> workflow) where TData : new()
        {
            _registry.RegisterWorkflow(workflow);
        }
    }
}