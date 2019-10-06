using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.Sqlite;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseSqlite(this WorkflowOptions options, string connectionString, bool canCreateDB)
        {
            options.UsePersistence(sp => new EntityFrameworkPersistenceProvider(new SqliteContextFactory(connectionString), canCreateDB, false));
            options.Services.AddTransient<IWorkflowPurger>(sp => new WorkflowPurger(new SqliteContextFactory(connectionString)));
            return options;
        }
    }
}
