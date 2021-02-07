using MongoDB.Driver;
using WorkflowCore.Models;
using WorkflowCore.Persistence.MongoDB.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public class MongoDBWorkflowOptions : WorkflowOptions
    {
        private readonly WorkflowOptions _options;
        private readonly string _mongoUrl;
        private readonly string _databaseName;

        public MongoDBWorkflowOptions(
            WorkflowOptions options, 
            string mongoUrl, 
            string databaseName)
            : base(options.Services)
        {
            _options = options;
            _mongoUrl = mongoUrl;
            _databaseName = databaseName;
        }

        public MongoDBWorkflowOptions WithQueueCache()
        {
            _options.UseQueueCacheProvider(sp =>
            {
                var client = new MongoClient(_mongoUrl);
                var db = client.GetDatabase(_databaseName);
                return new MongoQueueCache(db);
            });

            return this;
        }
    }
}