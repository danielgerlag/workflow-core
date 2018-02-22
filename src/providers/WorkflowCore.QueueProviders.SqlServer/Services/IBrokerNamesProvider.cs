#region using

using System;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    /// <summary>
    ///     Base interface for <see cref="BrokerNamesProvider" />
    /// </summary>
    public interface IBrokerNamesProvider
    {
        string WorkflowContractName { get; }
        string TargetEventServiceName { get; }
        string InitiatorEventServiceName { get; }
        string WorkflowQueueName { get; }
        string EventQueueName { get; }
        string TargetWorkflowServiceName { get; }
        string InitiatorWorkflowServiceName { get; }
        string EventContractName { get; }
        string EventMessageType { get; }
        string WorkflowMessageType { get; }
    }
}