using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Services;
using WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddWorkflow(this IServiceCollection services, Action<WorkflowOptions> setupAction = null)
        {
            WorkflowOptions options = new WorkflowOptions();
            if (setupAction != null)
                setupAction.Invoke(options);
            services.AddTransient<IPersistenceProvider>(options.PersistanceFactory);
            services.AddSingleton<IQueueProvider>(options.QueueFactory);
            services.AddSingleton<IDistributedLockProvider>(options.LockFactory);
            services.AddSingleton<IWorkflowRegistry, WorkflowRegistry>();
            services.AddSingleton<WorkflowOptions>(options);
            services.AddSingleton<IWorkflowHost, WorkflowHost>();
            services.AddTransient<IWorkflowExecutor, WorkflowExecutor>();
            services.AddTransient<IWorkflowBuilder, WorkflowBuilder>();
            services.AddTransient<IWorkflowThread, WorkflowThread>();
            services.AddTransient<IEventThread, EventThread>();
            services.AddTransient<IRunnablePoller, RunnablePoller>();
        }
    }
}

