using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.MySQL;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseMySQL(this WorkflowOptions options, string connectionString, bool canCreateDB, bool canMigrateDB, Action<MySqlDbContextOptionsBuilder> mysqlOptionsAction = null)
        {
            options.UseEntityFrameworkPersistence();
            options.Services.AddTransient<IWorkflowDbContextFactory>(_ => new MysqlContextFactory(connectionString, mysqlOptionsAction));

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
