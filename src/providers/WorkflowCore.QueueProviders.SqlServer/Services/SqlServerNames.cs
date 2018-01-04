#region using

using System;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    /// <summary>
    /// Build names for SSSB objects
    /// </summary>
    /// <remarks>
    /// Message type and contract are global, service name and queue different for every workflow host
    /// </remarks>
    public class SqlServerNames
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="workflowHostName"></param>
        public SqlServerNames(string workflowHostName)
        {
            WorkflowMessageType = "//workflow-core/workflow";
            EventMessageType = "//workflow-core/event";

            EventContractName = "//workflow-core/eventContract";
            WorkflowContractName = "//workflow-core/workflowContract";

            InitiatorEventServiceName = $"//workflow-core/{workflowHostName}/initiatorEventService";
            TargetEventServiceName = $"//workflow-core/{workflowHostName}/targetEventService";

            InitiatorWorkflowServiceName = $"//workflow-core/{workflowHostName}/initiatorWorkflowService";
            TargetWorkflowServiceName = $"//workflow-core/{workflowHostName}/targetWorkflowService";

            EventQueueName = $"//workflow-core/{workflowHostName}/eventQueue";
            WorkflowQueueName = $"//workflow-core/{workflowHostName}/workflowQueue";
        }

        public string WorkflowContractName { get; }

        public string TargetEventServiceName { get; }

        public string InitiatorEventServiceName { get; }

        public string WorkflowQueueName { get; }

        public string EventQueueName { get; }

        public string TargetWorkflowServiceName { get; }

        public string InitiatorWorkflowServiceName { get; }

        public string EventContractName { get; }

        public string EventMessageType { get; }

        public string WorkflowMessageType { get; }
    }
}