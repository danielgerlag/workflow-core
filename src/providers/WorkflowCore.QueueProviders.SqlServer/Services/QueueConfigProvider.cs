#region using

using System;
using System.Collections.Generic;
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
        private readonly Dictionary<QueueType, QueueConfig> _queues = new Dictionary<QueueType, QueueConfig>
        {
            [QueueType.Workflow] = new QueueConfig("workflow"),
            [QueueType.Event] = new QueueConfig("event"),
            [QueueType.Index] = new QueueConfig("indexq")
        };

        public QueueConfig GetByQueue(QueueType queue) => _queues[queue];
    }
}