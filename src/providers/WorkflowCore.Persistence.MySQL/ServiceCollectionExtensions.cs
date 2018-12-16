using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.MySQL;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseMySQL(this WorkflowOptions options, string connectionString, bool canCreateDB, Action<MySqlDbContextOptionsBuilder> mysqlOptionsAction = null)
        {
            options.UsePersistence(sp => new EntityFrameworkPersistenceProvider(new MysqlContextFactory(connectionString, mysqlOptionsAction), canCreateDB, false));
            return options;
        }
    }
}
