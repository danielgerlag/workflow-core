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
            Action<MongoClientSettings> configureClient = default,
            Func<Type, bool> serializerTypeFilter = null)
        {
            RegisterObjectSerializer(serializerTypeFilter);
            
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
            Func<IServiceProvider, IMongoDatabase> createDatabase,
            Func<Type, bool> serializerTypeFilter = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (createDatabase == null) throw new ArgumentNullException(nameof(createDatabase));

            RegisterObjectSerializer(serializerTypeFilter);
            
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
        
        private static void RegisterObjectSerializer(Func<Type, bool> serializerTypeFilter)
        {
            if (serializerTypeFilter != null)
            {
                MongoDB.Bson.Serialization.BsonSerializer.TryRegisterSerializer(
                    new MongoDB.Bson.Serialization.Serializers.ObjectSerializer(serializerTypeFilter));
            }
        }
    }
}
