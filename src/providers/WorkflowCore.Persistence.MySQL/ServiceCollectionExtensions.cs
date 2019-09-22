using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.MySQL;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseMySQL(this WorkflowOptions options, string connectionString, bool canCreateDB, bool canMigrateDB, Action<MySqlDbContextOptionsBuilder> mysqlOptionsAction = null)
        {
            options.UsePersistence(sp => new EntityFrameworkPersistenceProvider(new MysqlContextFactory(connectionString, mysqlOptionsAction), canCreateDB, canMigrateDB));
            options.Services.AddTransient<IWorkflowPurger>(sp => new WorkflowPurger(new MysqlContextFactory(connectionString, mysqlOptionsAction)));
            return options;
        }
    }
}
