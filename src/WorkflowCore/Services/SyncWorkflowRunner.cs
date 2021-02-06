using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Exceptions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services
{
    public class SyncWorkflowRunner : ISyncWorkflowRunner
    {
        private readonly IWorkflowHost _host;
        private readonly IWorkflowExecutor _executor;
        private readonly IDistributedLockProvider _lockService;
        private readonly IWorkflowRegistry _registry;
        private readonly IPersistenceProvider _persistenceStore;
        private readonly IExecutionPointerFactory _pointerFactory;
        private readonly IQueueProvider _queueService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public SyncWorkflowRunner(IWorkflowHost host, IWorkflowExecutor executor, IDistributedLockProvider lockService, IWorkflowRegistry registry, IPersistenceProvider persistenceStore, IExecutionPointerFactory pointerFactory, IQueueProvider queueService, IDateTimeProvider dateTimeProvider)
        {
            _host = host;
            _executor = executor;
            _lockService = lockService;
            _registry = registry;
            _persistenceStore = persistenceStore;
            _pointerFactory = pointerFactory;
            _queueService = queueService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<WorkflowInstance> RunWorkflowSync<TData>(string workflowId, int version, TData data, string reference, TimeSpan timeOut, bool persistSate = true)
            where TData : new()
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
                Status = WorkflowStatus.Suspended,
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

            var stopWatch = new Stopwatch();

            var id = Guid.NewGuid().ToString();

            if (persistSate)
                id = await _persistenceStore.CreateNewWorkflow(wf);
            else
                wf.Id = id;

            wf.Status = WorkflowStatus.Runnable;
            
            if (!await _lockService.AcquireLock(id, CancellationToken.None))
            {
                throw new InvalidOperationException();
            }

            try
            {
                stopWatch.Start();
                while ((wf.Status == WorkflowStatus.Runnable) && (timeOut.TotalMilliseconds > stopWatch.ElapsedMilliseconds))
                {
                    await _executor.Execute(wf);
                    if (persistSate)
                        await _persistenceStore.PersistWorkflow(wf);
                }
            }
            finally
            {
                stopWatch.Stop();
                await _lockService.ReleaseLock(id);
            }

            if (persistSate)
                await _queueService.QueueWork(id, QueueType.Index);

            return wf;
        }
    }
}