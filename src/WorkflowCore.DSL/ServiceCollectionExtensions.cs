using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Services.DefinitionStorage;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowDSL(this IServiceCollection services)
        {
            services.AddTransient<ITypeResolver, TypeResolver>();
            services.AddTransient<IDefinitionLoader, DefinitionLoader>();
            return services;
        }
    }
}

