using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.QueueProviders.SqlServer.Models
{
    public class QueueConfig
    {
        public QueueConfig(string msgType, string initiatorService, string targetService, string contractName, string queueName)
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
}
