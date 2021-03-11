using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using WorkflowCore.LockProviders.SqlServer;
using WorkflowCore.Models;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseSqlServerLocking(this WorkflowOptions options, string connectionString)
        {
            options.UseDistributedLockManager(sp => new SqlLockProvider(connectionString, sp.GetService<ILoggerFactory>()));
            return options;
        }
    }
}
