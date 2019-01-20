using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Providers.Redis.Services
{
    public class RedisPersistenceProvider : IPersistenceProvider
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly string _prefix;
        private const string WORKFLOW_SET = "workflows";
        private const string SUBSCRIPTION_SET = "events";
        private const string EVENT_SET = "events";
        private const string RUNNABLE_INDEX = "runnable";
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly IDatabase _redis;

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        public RedisPersistenceProvider(string connectionString, string prefix, ILoggerFactory logFactory)
        {
            _connectionString = connectionString;
            _prefix = prefix;
            _logger = logFactory.CreateLogger(GetType());
            _multiplexer = ConnectionMultiplexer.Connect(_connectionString);
            _redis = _multiplexer.GetDatabase();
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            throw new NotImplementedException();
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            var txn = _redis.CreateTransaction();
            var str = JsonConvert.SerializeObject(workflow, _serializerSettings);
            await txn.HashSetAsync($"{_prefix}.{WORKFLOW_SET}", workflow.Id, str);
            _redis.SortedSetScan
            txn.scan

            await txn.ExecuteAsync();
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip,
            int take)
        {
            throw new NotImplementedException();
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            throw new NotImplementedException();
        }

        public async Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<EventSubscription>> GetSubcriptions(string eventName, string eventKey, DateTime asOf)
        {
            throw new NotImplementedException();
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            throw new NotImplementedException();
        }

        public async Task<string> CreateEvent(Event newEvent)
        {
            throw new NotImplementedException();
        }

        public async Task<Event> GetEvent(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            throw new NotImplementedException();
        }

        public async Task MarkEventProcessed(string id)
        {
            throw new NotImplementedException();
        }

        public async Task MarkEventUnprocessed(string id)
        {
            throw new NotImplementedException();
        }

        public async Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            throw new NotImplementedException();
        }

        public void EnsureStoreExists()
        {
            throw new NotImplementedException();
        }
    }
}
