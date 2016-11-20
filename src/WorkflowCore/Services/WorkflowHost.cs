using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowHost : IWorkflowHost
    {

        protected readonly IPersistenceProvider _persistenceStore;        
        protected readonly IDistributedLockProvider _lockProvider;
        protected readonly IWorkflowRegistry _registry;
        protected readonly WorkflowOptions _options;
        protected readonly IQueueProvider _queueProvider;
        protected List<Thread> _threads = new List<Thread>();
        protected bool _shutdown = true;
        protected ILogger _logger;
        protected IServiceProvider _serviceProvider;
        protected Timer _pollTimer;

        public WorkflowHost(IPersistenceProvider persistenceStore, IQueueProvider queueProvider, WorkflowOptions options, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry, IDistributedLockProvider lockProvider)
        {
            _persistenceStore = persistenceStore;
            _queueProvider = queueProvider;
            _options = options;
            _logger = loggerFactory.CreateLogger<WorkflowHost>();
            _serviceProvider = serviceProvider;
            _registry = registry;
            _lockProvider = lockProvider;
            persistenceStore.EnsureStoreExists();
        }

        public async Task<string> StartWorkflow(string workflowId, int version, object data)
        {
            return await StartWorkflow<object>(workflowId, version, data);
        }

        public async Task<string> StartWorkflow<TData>(string workflowId, int version, TData data)
        {
            if (_shutdown)
                throw new Exception("Host is not running");

            var def = _registry.GetDefinition(workflowId, version);
            if (def == null)
                throw new Exception(String.Format("Workflow {0} version {1} is not registered", workflowId, version));

            var wf = new WorkflowInstance();
            wf.WorkflowDefinitionId = workflowId;
            wf.Version = version;
            wf.Data = data;
            wf.Description = def.Description;
            wf.NextExecution = 0;
            wf.ExecutionPointers.Add(new ExecutionPointer() { StepId = def.InitialStep, Active = true });
            string id = await _persistenceStore.CreateNewWorkflow(wf);
            await _queueProvider.QueueForProcessing(id);
            return id;
        }

        public void Start()
        {            
            _shutdown = false;
            _queueProvider.Start();
            for (int i = 0; i < _options.ThreadCount; i++)
            {
                _logger.LogInformation("Starting worker thread #{0}", i);
                Thread thread = new Thread(RunWorkflows);
                _threads.Add(thread);
                thread.Start();
            }

            _logger.LogInformation("Starting publish thread");
            Thread pubThread = new Thread(RunPublications);
            _threads.Add(pubThread);
            pubThread.Start();

            _pollTimer = new Timer(new TimerCallback(PollRunnables), null, TimeSpan.FromSeconds(0), _options.PollInterval);
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

            _logger.LogInformation("Stopping worker threads");
            foreach (Thread th in _threads)
                th.Join();
            _logger.LogInformation("Worker threads stopped");
            stashTask.Wait();
            _queueProvider.Stop();
        }


        public async Task SubscribeEvent(string workflowId, int stepId, string eventName, string eventKey)
        {
            _logger.LogDebug("Subscribing to event {0} {1} for workflow {2} step {3}", eventName, eventKey, workflowId, stepId);
            EventSubscription subscription = new EventSubscription();
            subscription.WorkflowId = workflowId;
            subscription.StepId = stepId;
            subscription.EventName = eventName;
            subscription.EventKey = eventKey;

            await _persistenceStore.CreateEventSubscription(subscription);
        }

        public async Task PublishEvent(string eventName, string eventKey, object eventData)
        {
            if (_shutdown)
                throw new Exception("Host is not running");

            _logger.LogDebug("Publishing event {0} {1}", eventName, eventKey);
            var subs = await _persistenceStore.GetSubcriptions(eventName, eventKey);
            foreach (var sub in subs.ToList())
            {
                EventPublication pub = new EventPublication();
                pub.Id = Guid.NewGuid();
                pub.EventData = eventData;
                pub.EventKey = eventKey;
                pub.EventName = eventName;
                pub.StepId = sub.StepId;
                pub.WorkflowId = sub.WorkflowId;
                await _queueProvider.QueueForPublishing(pub);
                await _persistenceStore.TerminateSubscription(sub.Id);                
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
                    var workflowId = _queueProvider.DequeueForProcessing().Result;
                    if (workflowId != null)
                    {
                        try
                        {
                            if (_lockProvider.AcquireLock(workflowId).Result)
                            {
                                var workflow = persistenceStore.GetWorkflowInstance(workflowId).Result;
                                try
                                {                                    
                                    workflowExecutor.Execute(workflow, persistenceStore, _options);
                                }
                                finally
                                {
                                    _lockProvider.ReleaseLock(workflowId);
                                    if (workflow.NextExecution.HasValue && workflow.NextExecution.Value < DateTime.Now.ToUniversalTime().Ticks)
                                        _queueProvider.QueueForProcessing(workflowId);
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Workflow locked {0}", workflowId);
                            }
                        }
                        catch (Exception ex)
                        {                            
                            _logger.LogError(ex.Message);
                        }
                    }
                    else
                    {
                        Thread.Sleep(_options.IdleTime); //no work
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
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
                    var pub = _queueProvider.DequeueForPublishing().Result;
                    if (pub != null)
                    {
                        try
                        {
                            if (_lockProvider.AcquireLock(pub.WorkflowId).Result)
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
                                    _logger.LogError(ex.Message);
                                    persistenceStore.CreateUnpublishedEvent(pub); //retry later                                    
                                }
                                finally
                                {
                                    _lockProvider.ReleaseLock(pub.WorkflowId);
                                    _queueProvider.QueueForProcessing(pub.WorkflowId);
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Workflow locked {0}", pub.WorkflowId);
                                persistenceStore.CreateUnpublishedEvent(pub); //retry later
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message);
                        }
                    }
                    else
                    {
                        Thread.Sleep(_options.IdleTime); //no work
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
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
                _logger.LogInformation("Polling for runnable workflows");
                IPersistenceProvider persistenceStore = _serviceProvider.GetService<IPersistenceProvider>();
                var runnables = persistenceStore.GetRunnableInstances().Result;
                foreach (var item in runnables)
                {
                    _logger.LogDebug("Got runnable instance {0}", item);
                    _queueProvider.QueueForProcessing(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            if (_lockProvider.AcquireLock("unpublished events").Result)
            {
                try
                {

                    _logger.LogInformation("Polling for unpublished events");
                    IPersistenceProvider persistenceStore = _serviceProvider.GetService<IPersistenceProvider>();
                    var events = persistenceStore.GetUnpublishedEvents().Result.ToList();
                    foreach (var item in events)
                    {
                        _logger.LogDebug("Got unpublished event {0} {1}", item.EventName, item.EventKey);
                        _queueProvider.QueueForPublishing(item).Wait();
                        persistenceStore.RemoveUnpublishedEvent(item.Id).Wait();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                finally
                {
                    _lockProvider.ReleaseLock("unpublished events").Wait();
                }
            }
            
        }

        private async Task StashUnpublishedEvents()
        {
            if (!_shutdown)
            {
                var pub = await _queueProvider.DequeueForPublishing();
                while (pub != null)
                {
                    await _persistenceStore.CreateUnpublishedEvent(pub);
                    pub = await _queueProvider.DequeueForPublishing();
                }
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
    }
}
