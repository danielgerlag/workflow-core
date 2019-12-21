using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Services.DefinitionStorage;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowDSL(this IServiceCollection services)
        {
            services.AddTransient<IDefinitionLoader, DefinitionLoader>();
            return services;
        }
    }
}

