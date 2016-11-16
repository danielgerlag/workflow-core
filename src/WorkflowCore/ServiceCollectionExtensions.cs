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
        public static void AddWorkflow(this IServiceCollection services, Action<WorkflowOptions> setupAction)
        {
            WorkflowOptions options = new WorkflowOptions();
            setupAction.Invoke(options);
            services.AddSingleton<IPersistenceProvider>(options.PersistanceFactory);
            services.AddSingleton<IConcurrencyProvider>(options.ConcurrencyFactory);
            services.AddSingleton<IWorkflowRegistry, WorkflowRegistry>();

            services.AddSingleton<IWorkflowRuntime, WorkflowRuntime>(sp => 
                new WorkflowRuntime(sp.GetService<IPersistenceProvider>(),
                    sp.GetService<IConcurrencyProvider>(),
                    options,
                    sp.GetService<ILoggerFactory>(),
                    sp,
                    sp.GetService<IWorkflowRegistry>())
            );
            services.AddTransient<IWorkflowExecutor, WorkflowExecutor>();
            services.AddTransient<IWorkflowBuilder, WorkflowBuilder>();


        }
    }
}

