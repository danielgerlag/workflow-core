using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class MongoPersistenceProvider : IPersistenceProvider
    {
        internal const string WorkflowCollectionName = "wfc.workflows";
        private readonly IMongoDatabase _database;

        public MongoPersistenceProvider(IMongoDatabase database)
        {
            _database = database;
            CreateIndexes(this);
        }

        static MongoPersistenceProvider()
        {
            ConventionRegistry.Register(
                "workflow.conventions",
                new ConventionPack
                {
                    new EnumRepresentationConvention(BsonType.String)
                }, t => t.FullName?.StartsWith("WorkflowCore") ?? false);

            BsonClassMap.RegisterClassMap<WorkflowInstance>(x =>
            {
                x.MapIdProperty(y => y.Id)
                    .SetIdGenerator(new StringObjectIdGenerator());
                x.MapProperty(y => y.Data)
                    .SetSerializer(new DataObjectSerializer());
                x.MapProperty(y => y.Description);
                x.MapProperty(y => y.Reference);
                x.MapProperty(y => y.WorkflowDefinitionId);
                x.MapProperty(y => y.Version);
                x.MapProperty(y => y.NextExecution);
                x.MapProperty(y => y.Status);
                x.MapProperty(y => y.CreateTime);
                x.MapProperty(y => y.CompleteTime);
                x.MapProperty(y => y.ExecutionPointers);
            });

            BsonClassMap.RegisterClassMap<EventSubscription>(x =>
            {
                x.MapIdProperty(y => y.Id)
                    .SetIdGenerator(new StringObjectIdGenerator());
                x.MapProperty(y => y.EventName);
                x.MapProperty(y => y.EventKey);
                x.MapProperty(y => y.StepId);
                x.MapProperty(y => y.ExecutionPointerId);
                x.MapProperty(y => y.WorkflowId);
                x.MapProperty(y => y.SubscribeAsOf);
                x.MapProperty(y => y.SubscriptionData);
                x.MapProperty(y => y.ExternalToken);
                x.MapProperty(y => y.ExternalTokenExpiry);
                x.MapProperty(y => y.ExternalWorkerId);
            });

            BsonClassMap.RegisterClassMap<Event>(x =>
            {
                x.MapIdProperty(y => y.Id)
                    .SetIdGenerator(new StringObjectIdGenerator());
                x.MapProperty(y => y.EventName);
                x.MapProperty(y => y.EventKey);
                x.MapProperty(y => y.EventData);
                x.MapProperty(y => y.EventTime);
                x.MapProperty(y => y.IsProcessed);
            });

            BsonClassMap.RegisterClassMap<ControlPersistenceData>(x => x.AutoMap());
            BsonClassMap.RegisterClassMap<SchedulePersistenceData>(x => x.AutoMap());
        }

        static bool indexesCreated = false;
        static void CreateIndexes(MongoPersistenceProvider instance)
        {
            if (!indexesCreated)
            {
                instance.WorkflowInstances.Indexes.CreateOne(new CreateIndexModel<WorkflowInstance>(
                    Builders<WorkflowInstance>.IndexKeys.Ascending(x => x.NextExecution),
                    new CreateIndexOptions {Background = true, Name = "idx_nextExec"}));

                instance.Events.Indexes.CreateOne(new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Ascending(x => x.IsProcessed),
                    new CreateIndexOptions {Background = true, Name = "idx_processed"}));

                indexesCreated = true;
            }
        }

        private IMongoCollection<WorkflowInstance> WorkflowInstances => _database.GetCollection<WorkflowInstance>(WorkflowCollectionName);

        private IMongoCollection<EventSubscription> EventSubscriptions => _database.GetCollection<EventSubscription>("wfc.subscriptions");

        private IMongoCollection<Event> Events => _database.GetCollection<Event>("wfc.events");

        private IMongoCollection<ExecutionError> ExecutionErrors => _database.GetCollection<ExecutionError>("wfc.execution_errors");

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            await WorkflowInstances.InsertOneAsync(workflow);
            return workflow.Id;
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            await WorkflowInstances.ReplaceOneAsync(x => x.Id == workflow.Id, workflow);
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt)
        {
            var now = asAt.ToUniversalTime().Ticks;
            var query = WorkflowInstances
                .Find(x => x.NextExecution.HasValue && (x.NextExecution <= now) && (x.Status == WorkflowStatus.Runnable))
                .Project(x => x.Id);

            return await query.ToListAsync();
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            var result = await WorkflowInstances.FindAsync(x => x.Id == Id);
            return await result.FirstAsync();
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                return new List<WorkflowInstance>();
            }

            var result = await WorkflowInstances.FindAsync(x => ids.Contains(x.Id));
            return await result.ToListAsync();
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            IMongoQueryable<WorkflowInstance> result = WorkflowInstances.AsQueryable();

            if (status.HasValue)
                result = result.Where(x => x.Status == status.Value);

            if (!String.IsNullOrEmpty(type))
                result = result.Where(x => x.WorkflowDefinitionId == type);

            if (createdFrom.HasValue)
                result = result.Where(x => x.CreateTime >= createdFrom.Value);

            if (createdTo.HasValue)
                result = result.Where(x => x.CreateTime <= createdTo.Value);

            return await result.Skip(skip).Take(take).ToListAsync();
        }

        public async Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            await EventSubscriptions.InsertOneAsync(subscription);
            return subscription.Id;
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            await EventSubscriptions.DeleteOneAsync(x => x.Id == eventSubscriptionId);
        }

        public async Task<EventSubscription> GetSubscription(string eventSubscriptionId)
        {
            var result = await EventSubscriptions.FindAsync(x => x.Id == eventSubscriptionId);
            return await result.FirstAsync();
        }

        public async Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf)
        {
            var query = EventSubscriptions
                .Find(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf && x.ExternalToken == null);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry)
        {
            var update = Builders<EventSubscription>.Update
                .Set(x => x.ExternalToken, token)
                .Set(x => x.ExternalTokenExpiry, expiry)
                .Set(x => x.ExternalWorkerId, workerId);

            var result = await EventSubscriptions.UpdateOneAsync(x => x.Id == eventSubscriptionId && x.ExternalToken == null, update);
            return (result.ModifiedCount > 0);
        }

        public async Task ClearSubscriptionToken(string eventSubscriptionId, string token)
        {
            var update = Builders<EventSubscription>.Update
                .Set(x => x.ExternalToken, null)
                .Set(x => x.ExternalTokenExpiry, null)
                .Set(x => x.ExternalWorkerId, null);

            await EventSubscriptions.UpdateOneAsync(x => x.Id == eventSubscriptionId && x.ExternalToken == token, update);
        }

        public void EnsureStoreExists()
        {

        }

        public async Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf)
        {
            var query = EventSubscriptions
                .Find(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf);

            return await query.ToListAsync();
        }

        public async Task<string> CreateEvent(Event newEvent)
        {
            await Events.InsertOneAsync(newEvent);
            return newEvent.Id;
        }

        public async Task<Event> GetEvent(string id)
        {
            var result = await Events.FindAsync(x => x.Id == id);
            return await result.FirstAsync();
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
        {
            var now = asAt.ToUniversalTime();
            var query = Events
                .Find(x => !x.IsProcessed && x.EventTime <= now)
                .Project(x => x.Id);

            return await query.ToListAsync();
        }

        public async Task MarkEventProcessed(string id)
        {
            var update = Builders<Event>.Update
                .Set(x => x.IsProcessed, true);

            await Events.UpdateOneAsync(x => x.Id == id, update);
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            var query = Events
                .Find(x => x.EventName == eventName && x.EventKey == eventKey && x.EventTime >= asOf)
                .Project(x => x.Id);

            return await query.ToListAsync();
        }

        public async Task MarkEventUnprocessed(string id)
        {
            var update = Builders<Event>.Update
                .Set(x => x.IsProcessed, false);

            await Events.UpdateOneAsync(x => x.Id == id, update);
        }

        public async Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            if (errors.Any())
                await ExecutionErrors.InsertManyAsync(errors);
        }
    }
}
