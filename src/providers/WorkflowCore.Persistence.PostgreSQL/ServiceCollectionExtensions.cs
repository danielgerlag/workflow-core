using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.PostgreSQL;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UsePostgreSQL(this WorkflowOptions options,
            string connectionString, bool canCreateDB, bool canMigrateDB, string schemaName="wfc")
        {
            options.UseEntityFrameworkPersistence();
            options.Services.AddTransient<IWorkflowDbContextFactory>(_ => new PostgresContextFactory(connectionString, schemaName));

            options.UsePersistence(sp =>
            {
                var modelConverterService = sp.GetRequiredService<ModelConverterService>();
                var contextFactory = sp.GetRequiredService<IWorkflowDbContextFactory>();
                
                return new EntityFrameworkPersistenceProvider(contextFactory, modelConverterService, canCreateDB, canMigrateDB);
            });
            return options;
        }
    }
}
