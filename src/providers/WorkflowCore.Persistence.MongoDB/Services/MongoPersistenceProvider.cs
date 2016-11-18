using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class MongoPersistenceProvider : IPersistenceProvider
    {

        private readonly IMongoDatabase _database;

        public MongoPersistenceProvider(IMongoDatabase database)
        {
            _database = database;
            CreateIndexes(WorkflowInstances);
        }

        static MongoPersistenceProvider()
        {
            //BsonSerializer.RegisterDiscriminatorConvention(typeof(WorkflowStep), new AssemblyQualifiedDiscriminatorConvention());
            //BsonSerializer.RegisterDiscriminatorConvention(typeof(Expression), new AssemblyQualifiedDiscriminatorConvention());
            //BsonSerializer.RegisterSerializer(new DataMappingSerializer());

            BsonClassMap.RegisterClassMap<WorkflowInstance>(x =>
            {
                x.MapIdProperty(y => y.Id)                    
                    .SetIdGenerator(new StringObjectIdGenerator());
                x.MapProperty(y => y.Data);
                x.MapProperty(y => y.Description);
                x.MapProperty(y => y.WorkflowDefinitionId);
                x.MapProperty(y => y.Version);
                x.MapProperty(y => y.NextExecution);
                x.MapProperty(y => y.ExecutionPointers);
            });

            BsonClassMap.RegisterClassMap<EventSubscription>(x =>
            {
                x.MapIdProperty(y => y.Id)
                    .SetIdGenerator(new StringObjectIdGenerator());
                x.MapProperty(y => y.EventName);
                x.MapProperty(y => y.EventKey);
                x.MapProperty(y => y.StepId);
                x.MapProperty(y => y.WorkflowId);
            });
        }

        static bool indexesCreated = false;
        static void CreateIndexes(IMongoCollection<WorkflowInstance> workflowInstances)
        {
            if (!indexesCreated)
            {
                workflowInstances.Indexes.CreateOne(Builders<WorkflowInstance>.IndexKeys.Ascending(x => x.NextExecution), new CreateIndexOptions() { Background = true, Name = "idx_nextExec" });
                indexesCreated = true;
            }
        }


        private IMongoCollection<WorkflowInstance> WorkflowInstances
        {
            get
            {
                return _database.GetCollection<WorkflowInstance>("wfc.workflows");
            }
        }

        private IMongoCollection<EventSubscription> EventSubscriptions
        {
            get
            {
                return _database.GetCollection<EventSubscription>("wfc.subscriptions");
            }
        }

        private IMongoCollection<EventPublication> UnpublishedEvents
        {
            get
            {
                return _database.GetCollection<EventPublication>("wfc.unpublishedEvents");
            }
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            await WorkflowInstances.InsertOneAsync(workflow);
            return workflow.Id;
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            await WorkflowInstances.ReplaceOneAsync(x => x.Id == workflow.Id, workflow);
        }

        public async Task<IEnumerable<string>> GetRunnableInstances()
        {
            var now = DateTime.Now.ToUniversalTime().Ticks;
            return WorkflowInstances.AsQueryable().Where(x => x.NextExecution.HasValue && x.NextExecution <= now).Select(x => x.Id).ToList();
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            return WorkflowInstances.AsQueryable().First(x => x.Id == Id);
        }


        public async Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            await EventSubscriptions.InsertOneAsync(subscription);
            return subscription.Id;
        }

        public async Task<IEnumerable<EventSubscription>> GetSubcriptions(string eventName, string eventKey)
        {
            return EventSubscriptions.AsQueryable()
                .Where(x => x.EventName == eventName && x.EventKey == eventKey).ToList();
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            await EventSubscriptions.DeleteOneAsync(x => x.Id == eventSubscriptionId);
        }

        public void EnsureStoreExists()
        {
            
        }

        public async Task CreateUnpublishedEvent(EventPublication publication)
        {
            await UnpublishedEvents.InsertOneAsync(publication);
        }

        public async Task<IEnumerable<EventPublication>> GetUnpublishedEvents()
        {
            return await UnpublishedEvents.AsQueryable().ToListAsync();
        }

        public async Task RemoveUnpublishedEvent(Guid id)
        {
            await UnpublishedEvents.DeleteOneAsync(x => x.Id == id);
        }
    }
}
