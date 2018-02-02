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
        /// Use SQL Server as a queue provider
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connectionString"></param>
        /// <param name="workflowHostName"></param>
        /// <param name="canMigrateDb">Autogenerate required service broker objects</param>
        /// <param name="canCreateDb"></param>
        /// <returns></returns>
        public static WorkflowOptions UseSqlServerBroker(this WorkflowOptions options, string connectionString, string workflowHostName,
            bool canMigrateDb = false, bool canCreateDb = false)
        {
            options.UseQueueProvider(sp =>
            {
                var opt = new SqlServerQueueProviderOption(connectionString, workflowHostName, canMigrateDb, canCreateDb);
                return new SqlServerQueueProvider(sp,opt);
            });
            return options;
        }

        /// <summary>
        /// Use SQL Server as a queue provider (use 'default' as workflowHostName)
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connectionString"></param>
        /// <param name="canMigrateDb">Autogenerate required service broker objects</param>
        /// <param name="canCreateDb"></param>
        /// <returns></returns>
        public static WorkflowOptions UseSqlServerBroker(this WorkflowOptions options, string connectionString, bool canMigrateDb = false, bool canCreateDb = false)
        {
            UseSqlServerBroker(options, connectionString, "default", canMigrateDb, canCreateDb);
            return options;
        }
    }
}