using System;

namespace WorkflowCore.QueueProviders.SqlServer.Models
{
    public class QueueConfig
    {
        public QueueConfig(string name)
        {
            MsgType = $"//workflow-core/{name}";
            InitiatorService = $"//workflow-core/initiator{name}Service";
            TargetService = $"//workflow-core/target{name}Service";
            ContractName = $"//workflow-core/{name}Contract";
            QueueName = $"//workflow-core/{name}Queue";
        }

        public string MsgType { get; }
        public string InitiatorService { get; }
        public string TargetService { get; }
        public string ContractName { get; }
        public string QueueName { get; }
    }
}
