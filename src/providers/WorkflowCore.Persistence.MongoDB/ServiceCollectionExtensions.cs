using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.MongoDB.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseMongoDB(this WorkflowOptions options, string mongoUrl, string databaseName)
        {
            options.UsePersistence(sp =>
            {
                var client = new MongoClient(mongoUrl);
                var db = client.GetDatabase(databaseName);
                return new MongoPersistenceProvider(db);
            });
            options.Services.AddTransient<IWorkflowPurger>(sp =>
            {
                var client = new MongoClient(mongoUrl);
                var db = client.GetDatabase(databaseName);
                return new WorkflowPurger(db);
            });
            return options;
        }
    }
}
