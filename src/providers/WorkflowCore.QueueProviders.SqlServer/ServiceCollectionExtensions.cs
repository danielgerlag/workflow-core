using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WorkflowCore.Models;

using WorkflowCore.QueueProviders.SqlServer.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseSqlServerQueue(this WorkflowOptions options, string connectionString, string workflowHostName, bool canCreateDB)
        {
            options.UseQueueProvider(sp => new SqlServerQueueProvider(connectionString, workflowHostName, canCreateDB));
            return options;
        }
    }
}
