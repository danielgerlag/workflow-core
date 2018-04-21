#region using

using System;
using System.Linq;
using WorkflowCore.Interface;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class BrokerNames
    {
        public BrokerNames(string msgType, string initiatorService, string targetService, string contractName, string queueName)
        {
            MsgType = msgType;
            InitiatorService = initiatorService;
            TargetService = targetService;
            ContractName = contractName;
            QueueName = queueName;
        }

        public string MsgType { get; }
        public string InitiatorService { get; }
        public string TargetService { get; }
        public string ContractName { get; }
        public string QueueName { get; }
    }
    /// <summary>
    /// Build names for SSSB objects
    /// </summary>
    /// <remarks>
    /// Message type and contract are global, service name and queue different for every workflow host
    /// </remarks>
    public class BrokerNamesProvider : IBrokerNamesProvider
    {
        readonly BrokerNames _workFlowNames;
        readonly BrokerNames _eventNames;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="workflowHostName"></param>
        public BrokerNamesProvider(string workflowHostName)
        {
            var workflowMessageType = "//workflow-core/workflow";
            var eventMessageType = "//workflow-core/event";

            var eventContractName = "//workflow-core/eventContract";
            var workflowContractName = "//workflow-core/workflowContract";

            var initiatorEventServiceName = $"//workflow-core/{workflowHostName}/initiatorEventService";
            var targetEventServiceName = $"//workflow-core/{workflowHostName}/targetEventService";

            var initiatorWorkflowServiceName = $"//workflow-core/{workflowHostName}/initiatorWorkflowService";
            var targetWorkflowServiceName = $"//workflow-core/{workflowHostName}/targetWorkflowService";

            var eventQueueName = $"//workflow-core/{workflowHostName}/eventQueue";
            var workflowQueueName = $"//workflow-core/{workflowHostName}/workflowQueue";
            
            _workFlowNames = new BrokerNames(workflowMessageType, initiatorWorkflowServiceName, targetWorkflowServiceName, workflowContractName, eventQueueName);
            _eventNames = new BrokerNames(eventMessageType, initiatorEventServiceName, targetEventServiceName, eventContractName, workflowQueueName);
        }

        public BrokerNames GetByQueue(QueueType queue)
        {
            switch (queue)
            {
                case QueueType.Workflow:
                    return _workFlowNames;
                case QueueType.Event:
                    return _eventNames;
                default:
                    throw new ArgumentOutOfRangeException(nameof(queue), queue, null);
            }

        }
    }
}