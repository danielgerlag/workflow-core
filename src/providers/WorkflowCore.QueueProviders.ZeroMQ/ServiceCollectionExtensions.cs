using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.QueueProviders.ZeroMQ.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseZeroMQ(this WorkflowOptions options, int port, IEnumerable<string> peers, bool canTakeWork = true)
        {
            options.UseQueueProvider(sp => new ZeroMQProvider(port, peers, canTakeWork));
            return options;
        }
    }
}

