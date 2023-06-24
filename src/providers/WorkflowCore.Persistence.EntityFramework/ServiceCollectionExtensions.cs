using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace WorkflowCore.Persistence.EntityFramework
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseEntityFrameworkPersistence(this WorkflowOptions options)
        {
            options.Services.AddTransient<IWorkflowPurger, WorkflowPurger>();
            options.Services.AddSingleton(new ModelConverterService(options.JsonSerializerSettings));

            return options;
        }
    }
}