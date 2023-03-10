using System;
using System.Linq;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

using Oracle.EntityFrameworkCore.Infrastructure;

using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.Oracle
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseOracle(this WorkflowOptions options, string connectionString, bool canCreateDB, bool canMigrateDB, Action<OracleDbContextOptionsBuilder> mysqlOptionsAction = null)
        {
            options.UsePersistence(sp => new EntityFrameworkPersistenceProvider(new OracleContextFactory(connectionString, mysqlOptionsAction), canCreateDB, canMigrateDB));
            options.Services.AddTransient<IWorkflowPurger>(sp => new WorkflowPurger(new OracleContextFactory(connectionString, mysqlOptionsAction)));
            return options;
        }
    }
}
