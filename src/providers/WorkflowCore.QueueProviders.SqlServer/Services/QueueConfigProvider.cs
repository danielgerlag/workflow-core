#region using

using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.SqlServer.Interfaces;
using WorkflowCore.QueueProviders.SqlServer.Models;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    
    /// <summary>
    /// Build names for SSSB objects
    /// </summary>
    /// <remarks>
    /// Message type and contract are global, service name and queue different for every workflow host
    /// </remarks>
    public class QueueConfigProvider : IQueueConfigProvider
    {
        private readonly QueueConfig _workflowQueueConfig;
        private readonly QueueConfig _eventQueueConfig;
                
        public QueueConfigProvider()
        {
            var workflowMessageType = "//workflow-core/workflow";
            var eventMessageType = "//workflow-core/event";

            var eventContractName = "//workflow-core/eventContract";
            var workflowContractName = "//workflow-core/workflowContract";

            var initiatorEventServiceName = $"//workflow-core/initiatorEventService";
            var targetEventServiceName = $"//workflow-core/targetEventService";

            var initiatorWorkflowServiceName = $"//workflow-core/initiatorWorkflowService";
            var targetWorkflowServiceName = $"//workflow-core/targetWorkflowService";

            var eventQueueName = $"//workflow-core/eventQueue";
            var workflowQueueName = $"//workflow-core/workflowQueue";
            
            _workflowQueueConfig = new QueueConfig(workflowMessageType, initiatorWorkflowServiceName, targetWorkflowServiceName, workflowContractName, eventQueueName);
            _eventQueueConfig = new QueueConfig(eventMessageType, initiatorEventServiceName, targetEventServiceName, eventContractName, workflowQueueName);
        }

        public QueueConfig GetByQueue(QueueType queue)
        {
            switch (queue)
            {
                case QueueType.Workflow:
                    return _workflowQueueConfig;
                case QueueType.Event:
                    return _eventQueueConfig;
                default:
                    throw new ArgumentOutOfRangeException(nameof(queue), queue, null);
            }
        }
    }
}