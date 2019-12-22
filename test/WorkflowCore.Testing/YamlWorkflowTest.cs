using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services.DefinitionStorage;

namespace WorkflowCore.Testing
{
    public abstract class YamlWorkflowTest : IDisposable
    {
        protected IWorkflowHost Host;
        protected IPersistenceProvider PersistenceProvider;
        protected IDefinitionLoader DefinitionLoader;
        protected List<StepError> UnhandledStepErrors = new List<StepError>();

        protected virtual void Setup()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            //loggerFactory.AddConsole(LogLevel.Debug);

            PersistenceProvider = serviceProvider.GetService<IPersistenceProvider>();
            DefinitionLoader = serviceProvider.GetService<IDefinitionLoader>();
            Host = serviceProvider.GetService<IWorkflowHost>();
            Host.OnStepError += Host_OnStepError;
            Host.Start();
        }

        private void Host_OnStepError(WorkflowInstance workflow, WorkflowStep step, Exception exception)
        {
            UnhandledStepErrors.Add(new StepError()
            {
                Exception = exception,
                Step = step,
                Workflow = workflow
            });
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow();
            services.AddWorkflowDSL();
        }

        public string StartWorkflow(string json, object data)
        {
            var def = DefinitionLoader.LoadDefinition(json, Deserializers.Yaml);
            var workflowId = Host.StartWorkflow(def.Id, data).Result;
            return workflowId;
        }

        protected void WaitForWorkflowToComplete(string workflowId, TimeSpan timeOut)
        {
            var status = GetStatus(workflowId);
            var counter = 0;
            while ((status == WorkflowStatus.Runnable) && (counter < (timeOut.TotalMilliseconds / 100)))
            {
                Thread.Sleep(100);
                counter++;
                status = GetStatus(workflowId);
            }
        }

        protected IEnumerable<EventSubscription> GetActiveSubscriptons(string eventName, string eventKey)
        {
            return PersistenceProvider.GetSubscriptions(eventName, eventKey, DateTime.MaxValue).Result;
        }

        protected void WaitForEventSubscription(string eventName, string eventKey, TimeSpan timeOut)
        {
            var counter = 0;
            while ((!GetActiveSubscriptons(eventName, eventKey).Any()) && (counter < (timeOut.TotalMilliseconds / 100)))
            {
                Thread.Sleep(100);
                counter++;
            }
        }

        protected WorkflowStatus GetStatus(string workflowId)
        {
            var instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            return instance.Status;
        }

        protected TData GetData<TData>(string workflowId)
        {
            var instance = PersistenceProvider.GetWorkflowInstance(workflowId).Result;
            return (TData)instance.Data;
        }

        public void Dispose()
        {
            Host.Stop();
        }
    }
    
}
