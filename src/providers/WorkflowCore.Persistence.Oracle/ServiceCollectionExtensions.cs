using System;
using Microsoft.Extensions.DependencyInjection;

using Oracle.EntityFrameworkCore.Infrastructure;

using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.Oracle
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseOracle(this WorkflowOptions options, string connectionString, bool canCreateDB, bool canMigrateDB, Action<OracleDbContextOptionsBuilder> oracleOptionsAction = null)
        {
            options.UsePersistence(sp => new EntityFrameworkPersistenceProvider(new OracleContextFactory(connectionString, oracleOptionsAction), canCreateDB, canMigrateDB));
            options.Services.AddTransient<IWorkflowPurger>(sp => new WorkflowPurger(new OracleContextFactory(connectionString, oracleOptionsAction)));
            return options;
        }
    }
}
