using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using System.Reflection;

namespace WorkflowCore.Services
{
    public class WorkflowHost : IWorkflowHost, IDisposable
    {                
        protected List<Thread> _threads = new List<Thread>();
        protected bool _shutdown = true;        
        protected IServiceProvider _serviceProvider;
        protected Timer _pollTimer;

        public event StepErrorEventHandler OnStepError;

        //public dependencies to allow for extension method access
        public IPersistenceProvider PersistenceStore { get; private set; }
        public IDistributedLockProvider LockProvider { get; private set; }
        public IWorkflowRegistry Registry { get; private set; }
        public WorkflowOptions Options { get; private set; }
        public IQueueProvider QueueProvider { get; private set; }
        public ILogger Logger { get; private set; }

        public WorkflowHost(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, WorkflowOptions options, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider)
        {
            PersistenceStore = persistenceStore;
            QueueProvider = queueProvider;
            Options = options;
            Logger = loggerFactory.CreateLogger<WorkflowHost>();
            _serviceProvider = serviceProvider;
            Registry = registry;
            LockProvider = lockProvider;
            persistenceStore.EnsureStoreExists();
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
            if (_shutdown)
                throw new Exception("Host is not running");

            var def = Registry.GetDefinition(workflowId, version);
            if (def == null)
                throw new Exception(String.Format("Workflow {0} version {1} is not registered", workflowId, version));

            var wf = new WorkflowInstance();
            wf.WorkflowDefinitionId = workflowId;
            wf.Version = def.Version;
            wf.Data = data;
            wf.Description = def.Description;
            wf.NextExecution = 0;
            wf.CreateTime = DateTime.Now.ToUniversalTime();
            wf.Status = WorkflowStatus.Runnable;

            if ((def.DataType != null) && (data == null))
                wf.Data = def.DataType.GetConstructor(new Type[] { }).Invoke(null);

            wf.ExecutionPointers.Add(new ExecutionPointer()
            {
                Id = Guid.NewGuid().ToString(),
                StepId = def.InitialStep,
                Active = true,
                ConcurrentFork = 1
            });
            string id = await PersistenceStore.CreateNewWorkflow(wf);
            await QueueProvider.QueueForProcessing(id);
            return id;
        }

        public void Start()
        {            
            _shutdown = false;
            QueueProvider.Start();
            LockProvider.Start();
            for (int i = 0; i < Options.ThreadCount; i++)
            {
                Logger.LogInformation("Starting worker thread #{0}", i);
                Thread thread = new Thread(RunWorkflows);
                _threads.Add(thread);
                thread.Start();
            }

            Logger.LogInformation("Starting publish thread");
            Thread pubThread = new Thread(RunPublications);
            _threads.Add(pubThread);
            pubThread.Start();

            _pollTimer = new Timer(new TimerCallback(PollRunnables), null, TimeSpan.FromSeconds(0), Options.PollInterval);
        }

        public void Stop()
        {
            _shutdown = true;

            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }

            var stashTask = StashUnpublishedEvents();

            Logger.LogInformation("Stopping worker threads");
            foreach (Thread th in _threads)
                th.Join();
            Logger.LogInformation("Worker threads stopped");
            stashTask.Wait();
            QueueProvider.Stop();
            LockProvider.Stop();
        }


        public async Task SubscribeEvent(string workflowId, int stepId, string eventName, string eventKey)
        {
            Logger.LogDebug("Subscribing to event {0} {1} for workflow {2} step {3}", eventName, eventKey, workflowId, stepId);
            EventSubscription subscription = new EventSubscription();
            subscription.WorkflowId = workflowId;
            subscription.StepId = stepId;
            subscription.EventName = eventName;
            subscription.EventKey = eventKey;

            await PersistenceStore.CreateEventSubscription(subscription);
        }

        public async Task PublishEvent(string eventName, string eventKey, object eventData)
        {
            if (_shutdown)
                throw new Exception("Host is not running");

            Logger.LogDebug("Publishing event {0} {1}", eventName, eventKey);
            var subs = await PersistenceStore.GetSubcriptions(eventName, eventKey);
            foreach (var sub in subs.ToList())
            {
                EventPublication pub = new EventPublication();
                pub.Id = Guid.NewGuid();
                pub.EventData = eventData;
                pub.EventKey = eventKey;
                pub.EventName = eventName;
                pub.StepId = sub.StepId;
                pub.WorkflowId = sub.WorkflowId;
                await QueueProvider.QueueForPublishing(pub);
                await PersistenceStore.TerminateSubscription(sub.Id);                
            }
        }

        /// <summary>
        /// Worker thread body
        /// </summary>        
        private void RunWorkflows()
        {
            IWorkflowExecutor workflowExecutor = _serviceProvider.GetService<IWorkflowExecutor>();
            IPersistenceProvider persistenceStore = _serviceProvider.GetService<IPersistenceProvider>();
            while (!_shutdown)
            {
                try
                {
                    var workflowId = QueueProvider.DequeueForProcessing().Result;
                    if (workflowId != null)
                    {
                        try
                        {
                            if (LockProvider.AcquireLock(workflowId).Result)
                            {
                                WorkflowInstance workflow = null;
                                try
                                {
                                    workflow = persistenceStore.GetWorkflowInstance(workflowId).Result;
                                    if (workflow.Status == WorkflowStatus.Runnable)
                                        workflowExecutor.Execute(workflow, persistenceStore, Options);
                                }
                                finally
                                {
                                    LockProvider.ReleaseLock(workflowId).Wait();
                                    if (workflow != null)
                                    {
                                        if ((workflow.Status == WorkflowStatus.Runnable) && workflow.NextExecution.HasValue && workflow.NextExecution.Value < DateTime.Now.ToUniversalTime().Ticks)
                                            QueueProvider.QueueForProcessing(workflowId);
                                    }
                                }
                            }
                            else
                            {
                                Logger.LogInformation("Workflow locked {0}", workflowId);
                            }
                        }
                        catch (Exception ex)
                        {                            
                            Logger.LogError(ex.Message);
                        }
                    }
                    else
                    {
                        Thread.Sleep(Options.IdleTime); //no work
                    }

                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message);
                }
            }
        }

        private void RunPublications()
        {
            IPersistenceProvider persistenceStore = _serviceProvider.GetService<IPersistenceProvider>();
            while (!_shutdown)
            {
                try
                {
                    var pub = QueueProvider.DequeueForPublishing().Result;
                    if (pub != null)
                    {
                        try
                        {
                            if (LockProvider.AcquireLock(pub.WorkflowId).Result)
                            {                                
                                try
                                {
                                    var workflow = persistenceStore.GetWorkflowInstance(pub.WorkflowId).Result;
                                    var pointers = workflow.ExecutionPointers.Where(p => p.EventName == pub.EventName && p.EventKey == p.EventKey && !p.EventPublished);
                                    foreach (var p in pointers)
                                    {
                                        p.EventData = pub.EventData;
                                        p.EventPublished = true;
                                        p.Active = true;
                                    }
                                    workflow.NextExecution = 0;
                                    persistenceStore.PersistWorkflow(workflow);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError(ex.Message);
                                    persistenceStore.CreateUnpublishedEvent(pub); //retry later                                    
                                }
                                finally
                                {
                                    LockProvider.ReleaseLock(pub.WorkflowId).Wait();
                                    QueueProvider.QueueForProcessing(pub.WorkflowId);
                                }
                            }
                            else
                            {
                                Logger.LogInformation("Workflow locked {0}", pub.WorkflowId);
                                persistenceStore.CreateUnpublishedEvent(pub); //retry later
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex.Message);
                        }
                    }
                    else
                    {
                        Thread.Sleep(Options.IdleTime); //no work
                    }

                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message);
                }
            }
        }

        /// <summary>
        /// Poll the persistence store for workflows ready to run.
        /// Poll the persistence store for stashed unpublished events
        /// </summary>        
        private void PollRunnables(object target)
        {   
            try
            {
                if (LockProvider.AcquireLock("poll runnables").Result)
                {
                    try
                    {
                        Logger.LogInformation("Polling for runnable workflows");
                        IPersistenceProvider persistenceStore = _serviceProvider.GetService<IPersistenceProvider>();
                        var runnables = persistenceStore.GetRunnableInstances().Result;
                        foreach (var item in runnables)
                        {
                            Logger.LogDebug("Got runnable instance {0}", item);
                            QueueProvider.QueueForProcessing(item);
                        }
                    }
                    finally
                    {
                        LockProvider.ReleaseLock("poll runnables").Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }

            try
            {
                if (LockProvider.AcquireLock("unpublished events").Result)
                {
                    try
                    {
                        Logger.LogInformation("Polling for unpublished events");
                        IPersistenceProvider persistenceStore = _serviceProvider.GetService<IPersistenceProvider>();
                        var events = persistenceStore.GetUnpublishedEvents().Result.ToList();
                        foreach (var item in events)
                        {
                            Logger.LogDebug("Got unpublished event {0} {1}", item.EventName, item.EventKey);
                            QueueProvider.QueueForPublishing(item).Wait();
                            persistenceStore.RemoveUnpublishedEvent(item.Id).Wait();
                        }
                    }
                    finally
                    {
                        LockProvider.ReleaseLock("unpublished events").Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }
        }

        private async Task StashUnpublishedEvents()
        {
            if (!_shutdown)
            {
                var pub = await QueueProvider.DequeueForPublishing();
                while (pub != null)
                {
                    await PersistenceStore.CreateUnpublishedEvent(pub);
                    pub = await QueueProvider.DequeueForPublishing();
                }
            }
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

        public async Task<bool> SuspendWorkflow(string workflowId)
        {
            if (LockProvider.AcquireLock(workflowId).Result)
            {
                try
                {
                    var wf = await PersistenceStore.GetWorkflowInstance(workflowId);
                    if (wf.Status == WorkflowStatus.Runnable)
                    {
                        wf.Status = WorkflowStatus.Suspended;
                        await PersistenceStore.PersistWorkflow(wf);
                        return true;
                    }
                    return false;
                }
                finally
                {
                    await LockProvider.ReleaseLock(workflowId);
                }
            }
            return false;
        }

        public async Task<bool> ResumeWorkflow(string workflowId)
        {
            if (LockProvider.AcquireLock(workflowId).Result)
            {
                try
                {
                    var wf = await PersistenceStore.GetWorkflowInstance(workflowId);
                    if (wf.Status == WorkflowStatus.Suspended)
                    {
                        wf.Status = WorkflowStatus.Runnable;
                        await PersistenceStore.PersistWorkflow(wf);
                        await QueueProvider.QueueForProcessing(workflowId);
                        return true;
                    }
                    return false;
                }
                finally
                {
                    await LockProvider.ReleaseLock(workflowId);
                }
            }
            return false;
        }

        public async Task<bool> TerminateWorkflow(string workflowId)
        {
            if (LockProvider.AcquireLock(workflowId).Result)
            {
                try
                {
                    var wf = await PersistenceStore.GetWorkflowInstance(workflowId);                    
                    wf.Status = WorkflowStatus.Terminated;
                    await PersistenceStore.PersistWorkflow(wf);
                    return true;                    
                }
                finally
                {
                    await LockProvider.ReleaseLock(workflowId);
                }
            }
            return false;
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
