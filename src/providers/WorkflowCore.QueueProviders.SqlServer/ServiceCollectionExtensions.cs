#region using

using System;
using System.Linq;

using WorkflowCore.Models;
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
        /// <returns></returns>
        public static WorkflowOptions UseSqlServerQueue(this WorkflowOptions options, string connectionString, string workflowHostName,
            bool canMigrateDb = false, bool canCreateDb = false)
        {
            options.UseQueueProvider(sp => new SqlServerQueueProvider(connectionString, workflowHostName, canMigrateDb, canCreateDb));
            return options;
        }

        /// <summary>
        /// Use SQL Server as a queue provider (use 'default' as workflowHostName)
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connectionString"></param>
        /// <param name="canMigrateDb">Autogenerate required service broker objects</param>
        /// <returns></returns>
        public static WorkflowOptions UseSqlServerQueue(this WorkflowOptions options, string connectionString, bool canMigrateDb = false, bool canCreateDb = false)
        {
            options.UseQueueProvider(sp => new SqlServerQueueProvider(connectionString, "default", canMigrateDb, canCreateDb));
            return options;
        }
    }
}