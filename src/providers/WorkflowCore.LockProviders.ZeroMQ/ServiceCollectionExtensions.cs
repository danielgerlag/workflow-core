using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.LockProviders.ZeroMQ.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseZeroMQLocking(this WorkflowOptions options, int port, IEnumerable<string> peers)
        {
            options.UseDistributedLockManager(sp => new ZeroMQLockProvider(port, peers, sp.GetService<ILoggerFactory>()));
            return options;
        }
    }
}

