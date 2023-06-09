using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Exceptions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

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

        public async Task<SyncWorkflowRunResult> RunWorkflowSync<TData>(string workflowId, int version, TData data, string reference, CancellationToken token, bool persistSate = true)
            where TData : new()
        {
            var wf = await PrepareWorkflowAsync(workflowId, version, data, reference, token, persistSate);

            
            var runTask = RunWorkflowSync<TData>(wf, token, persistSate);

            return new SyncWorkflowRunResult
            {
                InstanceId = wf.Id,
                CompletionTask = runTask,
            };
        }

        private async Task RunWorkflowSync<TData>(WorkflowInstance wf, CancellationToken token, bool persistSate)
            where TData : new()
        {
            wf.Status = WorkflowStatus.Runnable;

            if (!await _lockService.AcquireLock(wf.Id, CancellationToken.None))
            {
                throw new InvalidOperationException();
            }

            try
            {
                while ((wf.Status == WorkflowStatus.Runnable) && !token.IsCancellationRequested)
                {
                    await _executor.Execute(wf, token);
                    if (persistSate)
                        await _persistenceStore.PersistWorkflow(wf, token);
                }
            }
            finally
            {
                await _lockService.ReleaseLock(wf.Id);
            }

            if (persistSate)
                await _queueService.QueueWork(wf.Id, QueueType.Index);
        }

        private async Task<WorkflowInstance> PrepareWorkflowAsync<TData>(string workflowId, int version, TData data, string reference,
            CancellationToken token, bool persistSate) where TData : new()
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

            var id = Guid.NewGuid().ToString();

            if (persistSate)
                id = await _persistenceStore.CreateNewWorkflow(wf, token);

            wf.Id = id;

            return wf;
        }
    }
}