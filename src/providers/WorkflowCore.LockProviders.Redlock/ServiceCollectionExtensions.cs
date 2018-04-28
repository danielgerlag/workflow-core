using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.LockProviders.Redlock.Services;
using StackExchange.Redis;
using System.Net;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseRedlock(this WorkflowOptions options, params DnsEndPoint[] endpoints)
        {
            options.UseDistributedLockManager(sp => new RedlockProvider(endpoints));
            return options;
        }
    }
}
