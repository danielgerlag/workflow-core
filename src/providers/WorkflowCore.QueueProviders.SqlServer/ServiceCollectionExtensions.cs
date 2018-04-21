#region using

using System;
using System.Linq;

using WorkflowCore.Models;
using WorkflowCore.QueueProviders.SqlServer;
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
        /// <param name="opt"></param>
        /// <returns></returns>
        public static WorkflowOptions UseSqlServerBroker(this WorkflowOptions options, SqlServerQueueProviderOption opt)
        {
            options.UseQueueProvider(sp =>
            {
                var names = sp.GetService<IBrokerNamesProvider>()
                            ?? new BrokerNamesProvider(opt.WorkflowHostName);
                var sqlCommandExecutor = sp.GetService<ISqlCommandExecutor>()
                                         ?? new SqlCommandExecutor();
                var migrator = sp.GetService<ISqlServerQueueProviderMigrator>()
                               ?? new SqlServerQueueProviderMigrator(opt.ConnectionString, names, sqlCommandExecutor);

                return new SqlServerQueueProvider(opt, names, migrator, sqlCommandExecutor);
            });
            return options;
        }

        /// <summary>
        ///     Use SQL Server as a queue provider (use 'default' as workflowHostName)
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static WorkflowOptions UseSqlServerBroker(this WorkflowOptions options, string connectionString)
        {
            UseSqlServerBroker(options, new SqlServerQueueProviderOption {ConnectionString = connectionString});
            return options;
        }
    }
}