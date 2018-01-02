#region using

using System;
using System.Linq;

using Microsoft.Extensions.Logging;

using WorkflowCore.Models;
using WorkflowCore.QueueProviders.SqlServer.Services;

#endregion

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseSqlServerQueue(this WorkflowOptions options, string connectionString, string workflowHostName, bool canCreateDB)
        {
            options.UseQueueProvider(sp => new SqlServerQueueProvider(connectionString, workflowHostName, canCreateDB/*, sp.GetService<ILoggerFactory>()*/));
            return options;
        }
    }
}