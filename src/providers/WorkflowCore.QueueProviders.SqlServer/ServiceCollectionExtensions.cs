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
        public static WorkflowOptions UseSqlServerQueue(this WorkflowOptions options, string connectionString, string workflowHostName,
            bool canCreateDb = false)
        {
            options.UseQueueProvider(sp =>
                new SqlServerQueueProvider(connectionString, workflowHostName, canCreateDb /*, sp.GetService<ILoggerFactory>()*/));
            return options;
        }

        public static WorkflowOptions UseSqlServerQueue(this WorkflowOptions options, string connectionString, bool canCreateDb = false)
        {
            options.UseQueueProvider(sp =>
                new SqlServerQueueProvider(connectionString, "default", canCreateDb /*, sp.GetService<ILoggerFactory>()*/));
            return options;
        }
    }
}