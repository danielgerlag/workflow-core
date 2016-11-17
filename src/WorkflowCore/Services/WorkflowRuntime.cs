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
    public class WorkflowRuntime : IWorkflowRuntime
    {

        protected readonly IPersistenceProvider _persistenceStore;
        protected readonly IConcurrencyProvider _concurrencyProvider;
        protected readonly IWorkflowRegistry _registry;
        protected readonly WorkflowOptions _options;
        protected List<Thread> _threads = new List<Thread>();
        protected bool _shutdown = true;
        protected ILogger _logger;
        protected IServiceProvider _serviceProvider;
        protected Timer _pollTimer;

        public WorkflowRuntime(IPersistenceProvider persistenceStore, IConcurrencyProvider concurrencyProvider, WorkflowOptions options, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IWorkflowRegistry registry)
        {
            _persistenceStore = persistenceStore;
            _concurrencyProvider = concurrencyProvider;
            _options = options;
            _logger = loggerFactory.CreateLogger<WorkflowRuntime>();
            _serviceProvider = serviceProvider;
            _registry = registry;
            persistenceStore.EnsureStoreExists();
        }

        public async Task<string> StartWorkflow(string workflowId, int version, object data)
        {
            return await StartWorkflow<object>(workflowId, version, data);
        }

        public async Task<string> StartWorkflow<TData>(string workflowId, int version, TData data)
        {
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
            await _concurrencyProvider.EnqueueForProcessing(id);
            return id;
        }

        public void StartRuntime()
        {
            _concurrencyProvider.StartupNode();
            _shutdown = false;
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

        public void StopRuntime()
        {
            _shutdown = true;

            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }

            _logger.LogInformation("Stopping worker threads");
            foreach (Thread th in _threads)
                th.Join();

            _logger.LogInformation("Worker threads stopped");
            _concurrencyProvider.ShutdownNode();
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
            _logger.LogDebug("Publishing event {0} {1}", eventName, eventKey);
            var subs = await _persistenceStore.GetSubcriptions(eventName, eventKey);
            foreach (var sub in subs.ToList())
            {
                EventPublication pub = new EventPublication();
                pub.EventData = eventData;
                pub.EventKey = eventKey;
                pub.EventName = eventName;
                pub.StepId = sub.StepId;
                pub.WorkflowId = sub.WorkflowId;
                await _concurrencyProvider.EnqueueForPublishing(pub);
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
                    var workflowId = _concurrencyProvider.DequeueForProcessing().Result;
                    if (workflowId != null)
                    {
                        try
                        {
                            if (_concurrencyProvider.AcquireLock(workflowId).Result)
                            {
                                var workflow = persistenceStore.GetWorkflowInstance(workflowId).Result;
                                try
                                {                                    
                                    workflowExecutor.Execute(workflow, persistenceStore, _options);
                                }
                                finally
                                {
                                    _concurrencyProvider.ReleaseLock(workflowId);
                                    if (workflow.NextExecution.HasValue && workflow.NextExecution.Value < DateTime.Now.ToUniversalTime().Ticks)
                                        _concurrencyProvider.EnqueueForProcessing(workflowId);
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
                    var pub = _concurrencyProvider.DequeueForPublishing().Result;
                    if (pub != null)
                    {
                        try
                        {
                            if (_concurrencyProvider.AcquireLock(pub.WorkflowId).Result)
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
                                    _concurrencyProvider.EnqueueForDeferredPublishing(pub); //retry later                                    
                                }
                                finally
                                {
                                    _concurrencyProvider.ReleaseLock(pub.WorkflowId);
                                    _concurrencyProvider.EnqueueForProcessing(pub.WorkflowId);
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Workflow locked {0}", pub.WorkflowId);
                                _concurrencyProvider.EnqueueForDeferredPublishing(pub); //retry later
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
                    _concurrencyProvider.EnqueueForProcessing(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
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
