using MongoDB.Driver;
using System;
using System.Linq;
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
    }
}
