#region using

using System;
using WorkflowCore.Models;
using WorkflowCore.QueueProviders.SqlServer;
using WorkflowCore.QueueProviders.SqlServer.Interfaces;
using WorkflowCore.QueueProviders.SqlServer.Services;

#endregion

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {        
        /// <summary>
        ///     Use SQL Server as a queue provider
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static WorkflowOptions UseSqlServerBroker(this WorkflowOptions options, string connectionString, bool canCreateDb, bool canMigrateDb)
        {
            options.Services.AddTransient<IQueueConfigProvider, QueueConfigProvider>();
            options.Services.AddTransient<ISqlCommandExecutor, SqlCommandExecutor>();
            options.Services.AddTransient<ISqlServerQueueProviderMigrator>(sp => new SqlServerQueueProviderMigrator(connectionString, sp.GetService<IQueueConfigProvider>(), sp.GetService<ISqlCommandExecutor>()));

            var sqlOptions = new SqlServerQueueProviderOptions
            {
                ConnectionString = connectionString,
                CanCreateDb = canCreateDb,
                CanMigrateDb = canMigrateDb
            };

            options.UseQueueProvider(sp =>
            {
                return new SqlServerQueueProvider(sqlOptions, sp.GetService<IQueueConfigProvider>(), sp.GetService<ISqlServerQueueProviderMigrator>(), sp.GetService<ISqlCommandExecutor>());
            });

            return options;
        }
    }
}