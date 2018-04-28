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
    public class QueueConfigProvider : IQueueConfigProvider
    {
        private readonly QueueConfig _workflowQueueConfig;
        private readonly QueueConfig _eventQueueConfig;
                
        public QueueConfigProvider()
        {   
            _workflowQueueConfig = new QueueConfig("workflow");
            _eventQueueConfig = new QueueConfig("event");
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