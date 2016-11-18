using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.LockProviders.Redlock.Services;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseRedlock(this WorkflowOptions options, IConnectionMultiplexer connectionMultiplexer)
        {
            options.UseDistributedLockManager(sp => new RedlockProvider(connectionMultiplexer));
            return options;
        }
    }
}
