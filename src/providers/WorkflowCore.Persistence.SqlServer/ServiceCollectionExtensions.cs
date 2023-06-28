using System;
using System.Data.Common;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.SqlServer;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseSqlServer(this WorkflowOptions options, string connectionString, bool canCreateDB, bool canMigrateDB, Action<DbConnection> initAction = null)
        {
            options.UseEntityFrameworkPersistence();
            
            options.Services.AddTransient<IWorkflowDbContextFactory>(_ => new SqlContextFactory(connectionString, initAction));
            
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
