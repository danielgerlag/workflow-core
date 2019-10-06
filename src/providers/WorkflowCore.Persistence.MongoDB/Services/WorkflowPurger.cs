using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using WorkflowCore.Models;
using WorkflowCore.Interface;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class WorkflowPurger : IWorkflowPurger
    {
        private readonly IMongoDatabase _database;
        private IMongoCollection<WorkflowInstance> WorkflowInstances => _database.GetCollection<WorkflowInstance>(MongoPersistenceProvider.WorkflowCollectionName);


        public WorkflowPurger(IMongoDatabase database)
        {
            _database = database;
        }
        
        public async Task PurgeWorkflows(WorkflowStatus status, DateTime olderThan)
        {
            var olderThanUtc = olderThan.ToUniversalTime();
            await WorkflowInstances.DeleteManyAsync(x => x.Status == status && x.CompleteTime < olderThanUtc);
        }
    }
}