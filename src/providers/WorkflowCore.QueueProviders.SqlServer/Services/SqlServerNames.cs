#region using

using System;
using System.Linq;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    public class SqlServerNames
    {
        public SqlServerNames(string workflowHostName)
        {
            WorkflowMessageType = $"//workflow-core/{workflowHostName}/workflow";
            EventMessageType = $"//workflow-core/{workflowHostName}/event";

            EventContractName = $"//workflow-core/{workflowHostName}/eventContract";
            WorkflowContractName = $"//workflow-core/{workflowHostName}/workflowContract";

            InitiatorEventServiceName = $"//workflow-core/{workflowHostName}/initiatorEventService";
            TargetEventServiceName = $"//workflow-core/{workflowHostName}/targetEventService";

            InitiatorWorkflowServiceName = $"//workflow-core/{workflowHostName}/initiatorWorkflowService";
            TargetWorkflowServiceName = $"//workflow-core/{workflowHostName}/targetWorkflowService";

            EventQueueName = $"//workflow-core/{workflowHostName}/eventQueue";
            WorkflowQueueName = $"//workflow-core/{workflowHostName}/workflowQueue";
        }

        public string WorkflowContractName { get;  }

        public string TargetEventServiceName { get;  }

        public string InitiatorEventServiceName { get;  }

        public string WorkflowQueueName { get;  }

        public string EventQueueName { get;  }

        public string TargetWorkflowServiceName { get;  }

        public string InitiatorWorkflowServiceName { get;  }

        public string EventContractName { get;  }

        public string EventMessageType { get;  }

        public string WorkflowMessageType { get; }
    }
}