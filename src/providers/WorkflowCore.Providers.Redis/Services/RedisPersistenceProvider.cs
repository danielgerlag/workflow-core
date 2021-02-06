using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string SUBSCRIPTION_SET = "subscriptions";
        private const string EVENT_SET = "events";
        private const string RUNNABLE_INDEX = "runnable";
        private const string EVENTSLUG_INDEX = "eventslug";
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly IDatabase _redis;

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };
        private readonly bool _removeComplete;

        public RedisPersistenceProvider(string connectionString, string prefix, bool removeComplete, ILoggerFactory logFactory)
        {
            _connectionString = connectionString;
            _prefix = prefix;
            _logger = logFactory.CreateLogger(GetType());
            _multiplexer = ConnectionMultiplexer.Connect(_connectionString);
            _redis = _multiplexer.GetDatabase();
            _removeComplete = removeComplete;
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            workflow.Id = Guid.NewGuid().ToString();
            await PersistWorkflow(workflow);
            return workflow.Id;
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            var str = JsonConvert.SerializeObject(workflow, _serializerSettings);
            await _redis.HashSetAsync($"{_prefix}.{WORKFLOW_SET}", workflow.Id, str);

            if ((workflow.Status == WorkflowStatus.Runnable) && (workflow.NextExecution.HasValue))
                await _redis.SortedSetAddAsync($"{_prefix}.{WORKFLOW_SET}.{RUNNABLE_INDEX}", workflow.Id, workflow.NextExecution.Value);
            else
            {
                await _redis.SortedSetRemoveAsync($"{_prefix}.{WORKFLOW_SET}.{RUNNABLE_INDEX}", workflow.Id);
                if (_removeComplete && workflow.Status == WorkflowStatus.Complete)
                    await _redis.HashDeleteAsync($"{_prefix}.{WORKFLOW_SET}", workflow.Id);
            }
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt)
        {
            var result = new List<string>();
            var data = await _redis.SortedSetRangeByScoreAsync($"{_prefix}.{WORKFLOW_SET}.{RUNNABLE_INDEX}", -1, asAt.ToUniversalTime().Ticks);

            foreach (var item in data)
                result.Add(item);

            return result;
        }

        public Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip,
            int take)
        {
            throw new NotImplementedException();
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            var raw = await _redis.HashGetAsync($"{_prefix}.{WORKFLOW_SET}", Id);
            return JsonConvert.DeserializeObject<WorkflowInstance>(raw, _serializerSettings);
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                return new List<WorkflowInstance>();
            }

            var raw = await _redis.HashGetAsync($"{_prefix}.{WORKFLOW_SET}", Array.ConvertAll(ids.ToArray(), x => (RedisValue)x));
            return raw.Select(r => JsonConvert.DeserializeObject<WorkflowInstance>(r, _serializerSettings));
        }

        public async Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            subscription.Id = Guid.NewGuid().ToString();
            var str = JsonConvert.SerializeObject(subscription, _serializerSettings);
            await _redis.HashSetAsync($"{_prefix}.{SUBSCRIPTION_SET}", subscription.Id, str);
            await _redis.SortedSetAddAsync($"{_prefix}.{SUBSCRIPTION_SET}.{EVENTSLUG_INDEX}.{subscription.EventName}-{subscription.EventKey}", subscription.Id, subscription.SubscribeAsOf.Ticks);

            return subscription.Id;
        }

        public async Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf)
        {
            var result = new List<EventSubscription>();
            var data = await _redis.SortedSetRangeByScoreAsync($"{_prefix}.{SUBSCRIPTION_SET}.{EVENTSLUG_INDEX}.{eventName}-{eventKey}", -1, asOf.Ticks);

            foreach (var id in data)
            {
                var raw = await _redis.HashGetAsync($"{_prefix}.{SUBSCRIPTION_SET}", id);
                if (raw.HasValue)
                    result.Add(JsonConvert.DeserializeObject<EventSubscription>(raw, _serializerSettings));
            }

            return result;
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            var existingRaw = await _redis.HashGetAsync($"{_prefix}.{SUBSCRIPTION_SET}", eventSubscriptionId);
            var existing = JsonConvert.DeserializeObject<EventSubscription>(existingRaw, _serializerSettings);
            await _redis.HashDeleteAsync($"{_prefix}.{SUBSCRIPTION_SET}", eventSubscriptionId);
            await _redis.SortedSetRemoveAsync($"{_prefix}.{SUBSCRIPTION_SET}.{EVENTSLUG_INDEX}.{existing.EventName}-{existing.EventKey}", eventSubscriptionId);
        }

        public async Task<EventSubscription> GetSubscription(string eventSubscriptionId)
        {
            var raw = await _redis.HashGetAsync($"{_prefix}.{SUBSCRIPTION_SET}", eventSubscriptionId);
            return JsonConvert.DeserializeObject<EventSubscription>(raw, _serializerSettings);
        }

        public async Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf)
        {
            return (await GetSubscriptions(eventName, eventKey, asOf)).FirstOrDefault(sub => string.IsNullOrEmpty(sub.ExternalToken));
        }

        public async Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry)
        {
            var item = JsonConvert.DeserializeObject<EventSubscription>(await _redis.HashGetAsync($"{_prefix}.{SUBSCRIPTION_SET}", eventSubscriptionId), _serializerSettings);
            if (item.ExternalToken != null)
                return false;
            item.ExternalToken = token;
            item.ExternalWorkerId = workerId;
            item.ExternalTokenExpiry = expiry;
            var str = JsonConvert.SerializeObject(item, _serializerSettings);
            await _redis.HashSetAsync($"{_prefix}.{SUBSCRIPTION_SET}", eventSubscriptionId, str);
            return true;
        }

        public async Task ClearSubscriptionToken(string eventSubscriptionId, string token)
        {
            var item = JsonConvert.DeserializeObject<EventSubscription>(await _redis.HashGetAsync($"{_prefix}.{SUBSCRIPTION_SET}", eventSubscriptionId), _serializerSettings);
            if (item.ExternalToken != token)
                return;
            item.ExternalToken = null;
            item.ExternalWorkerId = null;
            item.ExternalTokenExpiry = null;
            var str = JsonConvert.SerializeObject(item, _serializerSettings);
            await _redis.HashSetAsync($"{_prefix}.{SUBSCRIPTION_SET}", eventSubscriptionId, str);
        }

        public async Task<string> CreateEvent(Event newEvent)
        {
            newEvent.Id = Guid.NewGuid().ToString();
            var str = JsonConvert.SerializeObject(newEvent, _serializerSettings);
            await _redis.HashSetAsync($"{_prefix}.{EVENT_SET}", newEvent.Id, str);
            await _redis.SortedSetAddAsync($"{_prefix}.{EVENT_SET}.{EVENTSLUG_INDEX}.{newEvent.EventName}-{newEvent.EventKey}", newEvent.Id, newEvent.EventTime.Ticks);

            if (newEvent.IsProcessed)
                await _redis.SortedSetRemoveAsync($"{_prefix}.{EVENT_SET}.{RUNNABLE_INDEX}", newEvent.Id);
            else
                await _redis.SortedSetAddAsync($"{_prefix}.{EVENT_SET}.{RUNNABLE_INDEX}", newEvent.Id, newEvent.EventTime.Ticks);

            return newEvent.Id;
        }

        public async Task<Event> GetEvent(string id)
        {
            var raw = await _redis.HashGetAsync($"{_prefix}.{EVENT_SET}", id);
            return JsonConvert.DeserializeObject<Event>(raw, _serializerSettings);
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
        {
            var result = new List<string>();
            var data = await _redis.SortedSetRangeByScoreAsync($"{_prefix}.{EVENT_SET}.{RUNNABLE_INDEX}", -1, asAt.Ticks);

            foreach (var item in data)
                result.Add(item);

            return result;
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            var result = new List<string>();
            var data = await _redis.SortedSetRangeByScoreAsync($"{_prefix}.{EVENT_SET}.{EVENTSLUG_INDEX}.{eventName}-{eventKey}", asOf.Ticks);

            foreach (var id in data)
                result.Add(id);

            return result;
        }

        public async Task MarkEventProcessed(string id)
        {
            var evt = await GetEvent(id);
            evt.IsProcessed = true;
            var str = JsonConvert.SerializeObject(evt, _serializerSettings);
            await _redis.HashSetAsync($"{_prefix}.{EVENT_SET}", evt.Id, str);
            await _redis.SortedSetRemoveAsync($"{_prefix}.{EVENT_SET}.{RUNNABLE_INDEX}", id);
        }

        public async Task MarkEventUnprocessed(string id)
        {
            var evt = await GetEvent(id);
            evt.IsProcessed = false;
            var str = JsonConvert.SerializeObject(evt, _serializerSettings);
            await _redis.HashSetAsync($"{_prefix}.{EVENT_SET}", evt.Id, str);
            await _redis.SortedSetAddAsync($"{_prefix}.{EVENT_SET}.{RUNNABLE_INDEX}", evt.Id, evt.EventTime.Ticks);
        }

        public Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            return Task.CompletedTask;
        }

        public void EnsureStoreExists()
        {
        }
    }
}
