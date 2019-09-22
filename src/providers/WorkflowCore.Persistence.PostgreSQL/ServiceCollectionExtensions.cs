using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.PostgreSQL;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UsePostgreSQL(this WorkflowOptions options,
            string connectionString, bool canCreateDB, bool canMigrateDB, string schemaName="wfc")
        {
            options.UsePersistence(sp => new EntityFrameworkPersistenceProvider(new PostgresContextFactory(connectionString, schemaName), canCreateDB, canMigrateDB));
            options.Services.AddTransient<IWorkflowPurger>(sp => new WorkflowPurger(new PostgresContextFactory(connectionString, schemaName)));
            return options;
        }
    }
}
