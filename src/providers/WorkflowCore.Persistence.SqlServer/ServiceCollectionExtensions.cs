using System;
using System.Data.Common;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.SqlServer;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseSqlServer(this WorkflowOptions options, string connectionString, bool canCreateDB, bool canMigrateDB, Action<DbConnection> initAction = null)
        {
            options.UsePersistence(sp => new EntityFrameworkPersistenceProvider(new SqlContextFactory(connectionString, initAction), canCreateDB, canMigrateDB));
            options.Services.AddTransient<IWorkflowPurger>(sp => new WorkflowPurger(new SqlContextFactory(connectionString, initAction)));
            return options;
        }
    }
}
