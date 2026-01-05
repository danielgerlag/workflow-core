using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.PostgreSQL;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        private static readonly Func<PostgresContextFactory, bool, bool, IPersistenceProvider> DefaultProviderFactory =
            (sqlContextFactory, canCreateDb, canMigrateDb) =>
                new EntityFrameworkPersistenceProvider(sqlContextFactory, canCreateDb, canMigrateDb);

        private static readonly Func<PostgresContextFactory, bool, bool, IPersistenceProvider> OptimizedProviderFactory =
            (sqlContextFactory, canCreateDb, canMigrateDb) =>
                new LargeDataOptimizedEntityFrameworkPersistenceProvider(sqlContextFactory, canCreateDb, canMigrateDb);

        public static WorkflowOptions UsePostgreSQL(this WorkflowOptions options, string connectionString, bool canCreateDB, bool canMigrateDB, string schemaName = "wfc") =>
            options.UsePostgreSQL(connectionString, canCreateDB, canMigrateDB, false, schemaName);

        public static WorkflowOptions UsePostgreSQL(this WorkflowOptions options, string connectionString, bool canCreateDB, bool canMigrateDB, bool largeDataOptimized, string schemaName="wfc")
        {
            var providerFactory = largeDataOptimized ? OptimizedProviderFactory : DefaultProviderFactory;

            options.UsePersistence(_ => providerFactory(new PostgresContextFactory(connectionString, schemaName), canCreateDB, canMigrateDB));
            options.Services.AddTransient<IWorkflowPurger>(sp => new WorkflowPurger(new PostgresContextFactory(connectionString, schemaName)));
            return options;
        }
    }
}
