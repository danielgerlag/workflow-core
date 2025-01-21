using System;
using System.Data.Common;
using Newtonsoft.Json;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework;
using WorkflowCore.Persistence.EntityFramework.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions ConfigureJsonSettings(this WorkflowOptions options, Action<JsonSerializerSettings> settings)
        {
            settings(ExtensionMethods.SerializerSettings);
            return options;
        }
    }
}
