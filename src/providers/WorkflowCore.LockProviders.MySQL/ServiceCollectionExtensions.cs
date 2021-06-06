using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using WorkflowCore.LockProviders.MySQL;
using WorkflowCore.Models;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseMySqlLocking(this WorkflowOptions options, string connectionString)
        {
            options.UseDistributedLockManager(sp => new MySqlLockProvider(connectionString, sp.GetService<ILoggerFactory>()));
            return options;
        }
    }
}
