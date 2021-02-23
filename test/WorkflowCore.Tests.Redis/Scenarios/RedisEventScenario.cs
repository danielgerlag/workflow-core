using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Providers.Redis.Services;
using WorkflowCore.Services;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.Tests.Redis.Scenarios
{
    [Collection("Redis collection")]
    public class RedisEventScenario : WorkflowTest<RedisEventScenario.EventWorkflow, RedisEventScenario.MyDataClass>
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x =>
            {
                x.UseQueueProvider(sp => new RedisQueueProvider(RedisDockerSetup.ConnectionString, "scenario-", sp.GetService<ILoggerFactory>()));
                x.UsePersistence(sp => new TransientMemoryPersistenceTestProvider(sp.GetRequiredService<ISingletonMemoryProvider>()));
            });
        }

        public RedisEventScenario()
        {
            this.Setup(false);
        }

        public class MyDataClass
        {
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            public string Value3 { get; set; }
        }
       
        public class EventWorkflow : IWorkflow<MyDataClass>
        {
            public string Id => "EventWorkflow";
            public int Version => 1;
            public void Build(IWorkflowBuilder<MyDataClass> builder)
            {
                builder
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor("MyEvent", data => data.Value1.ToString(), data => DateTime.Now)
                    .Output(data => data.Value2, step => step.EventData)
                    .Then(context => ExecutionResult.Next())
                    .Output(data => data.Value3, step => "OK");
            }
        }

        [Fact]
        public async Task RedisEventScenarioTest()
        {
            var eventKey = Guid.NewGuid().ToString();
            var workflowId = await StartWorkflowAsync(new MyDataClass() { Value1 = eventKey });
            WaitForEventSubscription("MyEvent", eventKey, TimeSpan.FromSeconds(30));
            Host.PublishEvent("MyEvent", eventKey,"DATA");
            
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(60));

            var events = await PersistenceProvider.GetEvents("MyEvent", eventKey, DateTime.MinValue);
            events.Count().Should().Be(1);
            var evnt = await PersistenceProvider.GetEvent(events.ElementAt(0));
            evnt.IsProcessed.Should().BeTrue();
            GetActiveSubscriptons("MyEvent", eventKey).Should().BeEmpty();
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
        }
    }
    

    public class TransientMemoryPersistenceTestProvider : IPersistenceProvider
    {
        private readonly ISingletonMemoryProvider _innerService;

        public TransientMemoryPersistenceTestProvider(ISingletonMemoryProvider innerService)
        {
            _innerService = innerService;
        }

        public Task<string> CreateEvent(Event newEvent) => _innerService.CreateEvent(newEvent);

        public Task<string> CreateEventSubscription(EventSubscription subscription) => _innerService.CreateEventSubscription(subscription);

        public Task<string> CreateNewWorkflow(WorkflowInstance workflow) => _innerService.CreateNewWorkflow(workflow);

        public void EnsureStoreExists() => _innerService.EnsureStoreExists();

        public Task<Event> GetEvent(string id) => _innerService.GetEvent(id);

        public virtual Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf) => _innerService.GetEvents(eventName, eventKey, asOf);

        public Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt) => _innerService.GetRunnableEvents(asAt);

        public Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt) => _innerService.GetRunnableInstances(asAt);

        private bool _mockedResponseSent;

        public Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf)
        {
            if ((new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().DeclaringType.Name.Contains("ProcessItem") && !_mockedResponseSent)
            {
                _mockedResponseSent = true;
                return Task.FromResult(new List<EventSubscription>() as IEnumerable<EventSubscription>);
            }

            return _innerService.GetSubscriptions(eventName, eventKey, asOf);
        }

        public Task<WorkflowInstance> GetWorkflowInstance(string Id) => _innerService.GetWorkflowInstance(Id);

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids) => _innerService.GetWorkflowInstances(ids);

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take) => _innerService.GetWorkflowInstances(status, type, createdFrom, createdTo, skip, take);

        public Task MarkEventProcessed(string id) => _innerService.MarkEventProcessed(id);

        public Task MarkEventUnprocessed(string id) => _innerService.MarkEventUnprocessed(id);

        public Task PersistErrors(IEnumerable<ExecutionError> errors) => _innerService.PersistErrors(errors);

        public virtual Task PersistWorkflow(WorkflowInstance workflow) => _innerService.PersistWorkflow(workflow);

        public Task TerminateSubscription(string eventSubscriptionId) => _innerService.TerminateSubscription(eventSubscriptionId);
        public Task<EventSubscription> GetSubscription(string eventSubscriptionId) => _innerService.GetSubscription(eventSubscriptionId);

        public Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf) => _innerService.GetFirstOpenSubscription(eventName, eventKey, asOf);

        public Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry) => _innerService.SetSubscriptionToken(eventSubscriptionId, token, workerId, expiry);

        public Task ClearSubscriptionToken(string eventSubscriptionId, string token) => _innerService.ClearSubscriptionToken(eventSubscriptionId, token);
    }
}