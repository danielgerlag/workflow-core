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
        private static readonly Func<SqlContextFactory, bool, bool, IPersistenceProvider> DefaultProviderFactory =
            (sqlContextFactory, canCreateDb, canMigrateDb) =>
                new EntityFrameworkPersistenceProvider(sqlContextFactory, canCreateDb, canMigrateDb);

        private static readonly Func<SqlContextFactory, bool, bool, IPersistenceProvider> OptimizedProviderFactory =
            (sqlContextFactory, canCreateDb, canMigrateDb) =>
                new LargeDataOptimizedEntityFrameworkPersistenceProvider(sqlContextFactory, canCreateDb, canMigrateDb);

        public static WorkflowOptions UseSqlServer(this WorkflowOptions options, string connectionString, bool canCreateDB, bool canMigrateDB, Action<DbConnection> initAction = null) =>
            options.UseSqlServer(connectionString, canCreateDB, canMigrateDB, false, initAction);

        public static WorkflowOptions UseSqlServer(this WorkflowOptions options, string connectionString, bool canCreateDB, bool canMigrateDB, bool largeDataOptimized, Action<DbConnection> initAction = null)
        {
            var providerFactory = largeDataOptimized ? OptimizedProviderFactory : DefaultProviderFactory;

            options.UsePersistence(_ => providerFactory(new SqlContextFactory(connectionString, initAction), canCreateDB, canMigrateDB));
            options.Services.AddTransient<IWorkflowPurger>(sp => new WorkflowPurger(new SqlContextFactory(connectionString, initAction)));
            return options;
        }
    }
}
