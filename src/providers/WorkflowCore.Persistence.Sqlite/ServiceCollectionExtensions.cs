using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.Sqlite;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseSqlite(this WorkflowOptions options, string connectionString, bool canCreateDB)
        {
            options.UseEntityFrameworkPersistence();
            
            options.Services.AddTransient<IWorkflowDbContextFactory>(_ => new SqliteContextFactory(connectionString));
            
            options.UsePersistence(sp =>
            {
                var modelConverterService = sp.GetRequiredService<ModelConverterService>();
                var contextFactory = sp.GetRequiredService<IWorkflowDbContextFactory>();

                return new EntityFrameworkPersistenceProvider(contextFactory, modelConverterService, canCreateDB, false);
            });

            return options;
        }
    }
}
