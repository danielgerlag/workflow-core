using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Exceptions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services
{
    public class WorkflowController : IWorkflowController
    {
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IWorkflowRegistry _registry;
        private readonly IQueueProvider _queueProvider;
        private readonly IExecutionPointerFactory _pointerFactory;
        private readonly ILifeCycleEventHub _eventHub;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly IDateTimeProvider _dateTimeProvider;

        public WorkflowController(IPersistenceProvider persistenceStore, IDistributedLockProvider lockProvider, IWorkflowRegistry registry, IQueueProvider queueProvider, IExecutionPointerFactory pointerFactory, ILifeCycleEventHub eventHub, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IDateTimeProvider dateTimeProvider)
        {
            _persistenceStore = persistenceStore;
            _lockProvider = lockProvider;
            _registry = registry;
            _queueProvider = queueProvider;
            _pointerFactory = pointerFactory;
            _eventHub = eventHub;
            _serviceProvider = serviceProvider;
            _logger = loggerFactory.CreateLogger<WorkflowController>();
            _dateTimeProvider = dateTimeProvider;
        }

        public Task<string> StartWorkflow(string workflowId, object data = null, string reference=null)
        {
            return StartWorkflow(workflowId, null, data, reference);
        }

        public Task<string> StartWorkflow(string workflowId, int? version, object data = null, string reference=null)
        {
            return StartWorkflow<object>(workflowId, version, data, reference);
        }

        public Task<string> StartWorkflow<TData>(string workflowId, TData data = null, string reference=null)
            where TData : class, new()
        {
            return StartWorkflow<TData>(workflowId, null, data, reference);
        }

        public async Task<string> StartWorkflow<TData>(string workflowId, int? version, TData data = null, string reference=null)
            where TData : class, new()
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
                CreateTime = _dateTimeProvider.UtcNow,
                Status = WorkflowStatus.Runnable,
                Reference = reference
            };

            if ((def.DataType != null) && (data == null))
            {
                if (typeof(TData) == def.DataType)
                    wf.Data = new TData();
                else
                    wf.Data = def.DataType.GetConstructor(new Type[0]).Invoke(new object[0]);
            }

            wf.ExecutionPointers.Add(_pointerFactory.BuildGenesisPointer(def));

            using (var scope = _serviceProvider.CreateScope())
            {
                var middlewareRunner = scope.ServiceProvider.GetRequiredService<IWorkflowMiddlewareRunner>();
                await middlewareRunner.RunPreMiddleware(wf, def);
            }

            string id = await _persistenceStore.CreateNewWorkflow(wf);
            await _queueProvider.QueueWork(id, QueueType.Workflow);
            await _queueProvider.QueueWork(id, QueueType.Index);
            await _eventHub.PublishNotification(new WorkflowStarted()
            {
                EventTimeUtc = _dateTimeProvider.UtcNow,
                Reference = reference,
                WorkflowInstanceId = id,
                WorkflowDefinitionId = def.Id,
                Version = def.Version
            });
            return id;
        }

        public async Task PublishEvent(string eventName, string eventKey, object eventData, DateTime? effectiveDate = null)
        {
            _logger.LogDebug("Creating event {0} {1}", eventName, eventKey);
            Event evt = new Event();

            if (effectiveDate.HasValue)
                evt.EventTime = effectiveDate.Value.ToUniversalTime();
            else
                evt.EventTime = _dateTimeProvider.UtcNow;

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
                    await _queueProvider.QueueWork(workflowId, QueueType.Index);
                    await _eventHub.PublishNotification(new WorkflowSuspended()
                    {
                        EventTimeUtc = _dateTimeProvider.UtcNow,
                        Reference = wf.Reference,
                        WorkflowInstanceId = wf.Id,
                        WorkflowDefinitionId = wf.WorkflowDefinitionId,
                        Version = wf.Version
                    });
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
                    await _queueProvider.QueueWork(workflowId, QueueType.Index);
                    await _eventHub.PublishNotification(new WorkflowResumed()
                    {
                        EventTimeUtc = _dateTimeProvider.UtcNow,
                        Reference = wf.Reference,
                        WorkflowInstanceId = wf.Id,
                        WorkflowDefinitionId = wf.WorkflowDefinitionId,
                        Version = wf.Version
                    });
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
                await _queueProvider.QueueWork(workflowId, QueueType.Index);
                await _eventHub.PublishNotification(new WorkflowTerminated()
                {
                    EventTimeUtc = _dateTimeProvider.UtcNow,
                    Reference = wf.Reference,
                    WorkflowInstanceId = wf.Id,
                    WorkflowDefinitionId = wf.WorkflowDefinitionId,
                    Version = wf.Version
                });
                return true;
            }
            finally
            {
                await _lockProvider.ReleaseLock(workflowId);
            }
        }

        public void RegisterWorkflow<TWorkflow>()
            where TWorkflow : IWorkflow
        {
            TWorkflow wf = ActivatorUtilities.CreateInstance<TWorkflow>(_serviceProvider);
            _registry.RegisterWorkflow(wf);
        }

        public void RegisterWorkflow<TWorkflow, TData>()
            where TWorkflow : IWorkflow<TData>
            where TData : new()
        {
            TWorkflow wf = ActivatorUtilities.CreateInstance<TWorkflow>(_serviceProvider);
            _registry.RegisterWorkflow<TData>(wf);
        }
    }
}
