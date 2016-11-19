using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.LockProviders.ZooKeeper.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseZooKeeperLocks(this WorkflowOptions options, string connectionString)
        {
            throw new NotImplementedException();
            //options.UseDistributedLockManager(sp => new ZooKeeperLockProvider(connectionString));
            //return options;
        }
    }
}
