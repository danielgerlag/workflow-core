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

        public Task<WorkflowInstance> RunWorkflowSync<TData>(string workflowId, int version, TData data,
            string reference, TimeSpan timeOut, bool persistState = true)
            where TData : new()
        {
            return RunWorkflowSync(workflowId, version, data, reference, new CancellationTokenSource(timeOut).Token,
                persistState);
        }

        public async Task<WorkflowInstance> RunWorkflowSync<TData>(string workflowId, int version, TData data, string reference, CancellationToken token, bool persistState = true)
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

            var id = Guid.NewGuid().ToString();

            if (persistState)
                id = await _persistenceStore.CreateNewWorkflow(wf, token);
            else
                wf.Id = id;

            return await RunWorkflowInstanceSync(wf, token, persistState);
        }

        public Task<WorkflowInstance> ResumeWorkflowSync(string workflowId, TimeSpan timeOut, bool persistState = true)
        {
            return ResumeWorkflowSync(workflowId, new CancellationTokenSource(timeOut).Token, persistState);
        }

        public async Task<WorkflowInstance> ResumeWorkflowSync(string workflowId, CancellationToken token, bool persistState = true)
        {
            WorkflowInstance wf = await _persistenceStore.GetWorkflowInstance(workflowId);

            return await RunWorkflowInstanceSync(wf, token, persistState);
        }

        private async Task<WorkflowInstance> RunWorkflowInstanceSync(WorkflowInstance wf, CancellationToken token, bool persistState)
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
                    if (persistState)
                        await _persistenceStore.PersistWorkflow(wf, token);
                }
            }
            finally
            {
                await _lockService.ReleaseLock(wf.Id);
            }

            if (persistState)
                await _queueService.QueueWork(wf.Id, QueueType.Index);

            return wf;
        }
    }
}