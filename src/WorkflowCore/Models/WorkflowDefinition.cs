using System;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
#endif

namespace WorkflowCore.Models
{
    public class WorkflowDefinition
    {
        public string Id { get; set; }

        public int Version { get; set; }

        public string Description { get; set; }

        public WorkflowStepCollection Steps { get; set; } = new WorkflowStepCollection();

#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        public Type DataType { get; set; }

        public WorkflowErrorHandling DefaultErrorBehavior { get; set; }

        public Type OnPostMiddlewareError { get; set; }
        public Type OnExecuteMiddlewareError { get; set; }

        public TimeSpan? DefaultErrorRetryInterval { get; set; }
    }

#if NET8_0_OR_GREATER
    [JsonConverter(typeof(JsonStringEnumConverter<WorkflowErrorHandling>))]
#endif
    public enum WorkflowErrorHandling
    {
        Retry = 0,
        Suspend = 1,
        Terminate = 2,
        Compensate = 3
    }
}
