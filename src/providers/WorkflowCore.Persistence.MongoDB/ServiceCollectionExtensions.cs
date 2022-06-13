using MongoDB.Driver;
using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.MongoDB.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseMongoDB(
            this WorkflowOptions options, 
            string mongoUrl, 
            string databaseName, 
            Action<MongoClientSettings> configureClient = default)
        {
            options.UsePersistence(sp =>
            {
                var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoUrl);
                configureClient?.Invoke(mongoClientSettings);
                var client = new MongoClient(mongoClientSettings);
                var db = client.GetDatabase(databaseName);
                return new MongoPersistenceProvider(db);
            });
            options.Services.AddTransient<IWorkflowPurger>(sp =>
            {
                var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoUrl);
                configureClient?.Invoke(mongoClientSettings);
                var client = new MongoClient(mongoClientSettings);
                var db = client.GetDatabase(databaseName);
                return new WorkflowPurger(db);
            });
            return options;
        }

        public static WorkflowOptions UseMongoDB(
            this WorkflowOptions options, 
            Func<IServiceProvider, IMongoDatabase> createDatabase)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (createDatabase == null) throw new ArgumentNullException(nameof(createDatabase));

            options.UsePersistence(sp =>
            {
                var db = createDatabase(sp);
                return new MongoPersistenceProvider(db);
            });
            options.Services.AddTransient<IWorkflowPurger>(sp =>
            {
                var db = createDatabase(sp);
                return new WorkflowPurger(db);
            });

            return options;
        }
    }
}
