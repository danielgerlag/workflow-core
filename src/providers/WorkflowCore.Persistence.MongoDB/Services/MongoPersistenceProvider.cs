using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using System.Threading;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class MongoPersistenceProvider : IPersistenceProvider
    {
        internal const string WorkflowCollectionName = "wfc.workflows";
        private readonly IMongoDatabase _database;

        public MongoPersistenceProvider(IMongoDatabase database)
        {
            _database = database;
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
                x.MapProperty(y => y.Status)
                    .SetSerializer(new EnumSerializer<WorkflowStatus>(BsonType.String));
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
            BsonClassMap.RegisterClassMap<IteratorPersistenceData>(x => x.AutoMap());
            BsonClassMap.RegisterClassMap<ScheduledCommand>(x => x.AutoMap())
                .SetIgnoreExtraElements(true);
        }

        static bool indexesCreated = false;
        static void CreateIndexes(MongoPersistenceProvider instance)
        {
            if (!indexesCreated)
            {
                instance.WorkflowInstances.Indexes.CreateOne(new CreateIndexModel<WorkflowInstance>(
                    Builders<WorkflowInstance>.IndexKeys
                        .Ascending(x => x.NextExecution)
                        .Ascending(x => x.Status)
                        .Ascending(x => x.Id),
                    new CreateIndexOptions {Background = true, Name = "idx_nextExec_v2"}));

                instance.Events.Indexes.CreateOne(new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys.Ascending(x => x.IsProcessed),
                    new CreateIndexOptions {Background = true, Name = "idx_processed"}));

                instance.Events.Indexes.CreateOne(new CreateIndexModel<Event>(
                    Builders<Event>.IndexKeys
                        .Ascending(x => x.EventName)
                        .Ascending(x => x.EventKey)
                        .Ascending(x => x.EventTime),
                    new CreateIndexOptions { Background = true, Name = "idx_namekey" }));

                instance.EventSubscriptions.Indexes.CreateOne(new CreateIndexModel<EventSubscription>(
                    Builders<EventSubscription>.IndexKeys
                        .Ascending(x => x.EventName)
                        .Ascending(x => x.EventKey),
                    new CreateIndexOptions { Background = true, Name = "idx_namekey" }));

                instance.ScheduledCommands.Indexes.CreateOne(new CreateIndexModel<ScheduledCommand>(
                    Builders<ScheduledCommand>.IndexKeys
                        .Descending(x => x.ExecuteTime),
                    new CreateIndexOptions { Background = true, Name = "idx_exectime" }));

                instance.ScheduledCommands.Indexes.CreateOne(new CreateIndexModel<ScheduledCommand>(
                    Builders<ScheduledCommand>.IndexKeys
                        .Ascending(x => x.CommandName)
                        .Ascending(x => x.Data),
                    new CreateIndexOptions { Background = true, Unique = true, Name = "idx_key" }));

                indexesCreated = true;
            }
        }

        private IMongoCollection<WorkflowInstance> WorkflowInstances => _database.GetCollection<WorkflowInstance>(WorkflowCollectionName);

        private IMongoCollection<EventSubscription> EventSubscriptions => _database.GetCollection<EventSubscription>("wfc.subscriptions");

        private IMongoCollection<Event> Events => _database.GetCollection<Event>("wfc.events");

        private IMongoCollection<ExecutionError> ExecutionErrors => _database.GetCollection<ExecutionError>("wfc.execution_errors");

        private IMongoCollection<ScheduledCommand> ScheduledCommands => _database.GetCollection<ScheduledCommand>("wfc.scheduled_commands");

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default)
        {
            await WorkflowInstances.InsertOneAsync(workflow, cancellationToken: cancellationToken);
            return workflow.Id;
        }

        public async Task PersistWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default)
        {
            await WorkflowInstances.ReplaceOneAsync(x => x.Id == workflow.Id, workflow, cancellationToken: cancellationToken);
        }

        public async Task PersistWorkflow(WorkflowInstance workflow, List<EventSubscription> subscriptions, CancellationToken cancellationToken = default)
        {
            if (subscriptions == null || subscriptions.Count < 1)
            {
                await PersistWorkflow(workflow, cancellationToken);
                return;
            }

            using (var session = await _database.Client.StartSessionAsync(cancellationToken: cancellationToken))
            {
                session.StartTransaction();
                await PersistWorkflow(workflow, cancellationToken);
                await EventSubscriptions.InsertManyAsync(subscriptions, cancellationToken: cancellationToken);
                await session.CommitTransactionAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt, CancellationToken cancellationToken = default)
        {
            var now = asAt.ToUniversalTime().Ticks;
            var query = WorkflowInstances
                .Find(x => x.NextExecution.HasValue && (x.NextExecution <= now) && (x.Status == WorkflowStatus.Runnable))
                .Project(x => x.Id);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id, CancellationToken cancellationToken = default)
        {
            var result = await WorkflowInstances.FindAsync(x => x.Id == Id, cancellationToken: cancellationToken);
            return await result.FirstAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
            {
                return new List<WorkflowInstance>();
            }

            var result = await WorkflowInstances.FindAsync(x => ids.Contains(x.Id), cancellationToken: cancellationToken);
            return await result.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            IQueryable<WorkflowInstance> result = WorkflowInstances.AsQueryable();

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

        public async Task<string> CreateEventSubscription(EventSubscription subscription, CancellationToken cancellationToken = default)
        {
            await EventSubscriptions.InsertOneAsync(subscription, cancellationToken: cancellationToken);
            return subscription.Id;
        }

        public async Task TerminateSubscription(string eventSubscriptionId, CancellationToken cancellationToken = default)
        {
            await EventSubscriptions.DeleteOneAsync(x => x.Id == eventSubscriptionId, cancellationToken);
        }

        public async Task<EventSubscription> GetSubscription(string eventSubscriptionId, CancellationToken cancellationToken = default)
        {
            var result = await EventSubscriptions.FindAsync(x => x.Id == eventSubscriptionId, cancellationToken: cancellationToken);
            return await result.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default)
        {
            var query = EventSubscriptions
                .Find(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf && x.ExternalToken == null);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken cancellationToken = default)
        {
            var update = Builders<EventSubscription>.Update
                .Set(x => x.ExternalToken, token)
                .Set(x => x.ExternalTokenExpiry, expiry)
                .Set(x => x.ExternalWorkerId, workerId);

            var result = await EventSubscriptions.UpdateOneAsync(x => x.Id == eventSubscriptionId && x.ExternalToken == null, update, cancellationToken: cancellationToken);
            return (result.ModifiedCount > 0);
        }

        public async Task ClearSubscriptionToken(string eventSubscriptionId, string token, CancellationToken cancellationToken = default)
        {
            var update = Builders<EventSubscription>.Update
                .Set(x => x.ExternalToken, null)
                .Set(x => x.ExternalTokenExpiry, null)
                .Set(x => x.ExternalWorkerId, null);

            await EventSubscriptions.UpdateOneAsync(x => x.Id == eventSubscriptionId && x.ExternalToken == token, update, cancellationToken: cancellationToken);
        }

        public void EnsureStoreExists()
        {
            CreateIndexes(this);
        }

        public async Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default)
        {
            var query = EventSubscriptions
                .Find(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<string> CreateEvent(Event newEvent, CancellationToken cancellationToken = default)
        {
            await Events.InsertOneAsync(newEvent, cancellationToken: cancellationToken);
            return newEvent.Id;
        }

        public async Task<Event> GetEvent(string id, CancellationToken cancellationToken = default)
        {
            var result = await Events.FindAsync(x => x.Id == id, cancellationToken: cancellationToken);
            return await result.FirstAsync(cancellationToken);
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt, CancellationToken cancellationToken = default)
        {
            var now = asAt.ToUniversalTime();
            var query = Events
                .Find(x => !x.IsProcessed && x.EventTime <= now)
                .Project(x => x.Id);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task MarkEventProcessed(string id, CancellationToken cancellationToken = default)
        {
            var update = Builders<Event>.Update
                .Set(x => x.IsProcessed, true);

            await Events.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken)
        {
            var query = Events
                .Find(x => x.EventName == eventName && x.EventKey == eventKey && x.EventTime >= asOf)
                .Project(x => x.Id);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task MarkEventUnprocessed(string id, CancellationToken cancellationToken = default)
        {
            var update = Builders<Event>.Update
                .Set(x => x.IsProcessed, false);

            await Events.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
        }

        public async Task PersistErrors(IEnumerable<ExecutionError> errors, CancellationToken cancellationToken = default)
        {
            if (errors.Any())
                await ExecutionErrors.InsertManyAsync(errors, cancellationToken: cancellationToken);
        }

        public bool SupportsScheduledCommands => true;

        public async Task ScheduleCommand(ScheduledCommand command)
        {
            try
            {
                await ScheduledCommands.InsertOneAsync(command);
            }
            catch (MongoWriteException ex)
            {
                if (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
                     return;
                throw;
            }
            catch (MongoBulkWriteException ex)
            {
                if (ex.WriteErrors.All(x => x.Category == ServerErrorCategory.DuplicateKey))
                    return;
                throw;
            }
        }

        public async Task ProcessCommands(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default)
        {
            var cursor = await ScheduledCommands.FindAsync(x => x.ExecuteTime < asOf.UtcDateTime.Ticks);
            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var command in cursor.Current)
                {
                    try
                    {
                        await action(command);
                        await ScheduledCommands.DeleteOneAsync(x => x.CommandName == command.CommandName && x.Data == command.Data);
                    }
                    catch (Exception)
                    {
                        //TODO: add logger
                    }
                }
            }
        }
    }
}
