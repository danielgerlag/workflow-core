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
        private readonly Dictionary<(QueueType, QueuePriority), QueueConfig> _queues = new Dictionary<(QueueType, QueuePriority), QueueConfig>
        {
            [(QueueType.Workflow, QueuePriority.Normal)] = new QueueConfig("workflow"),
            [(QueueType.Workflow, QueuePriority.High)] = new QueueConfig("workflowhigh"),
            [(QueueType.Event, QueuePriority.Normal)] = new QueueConfig("event"),
            [(QueueType.Index, QueuePriority.Normal)] = new QueueConfig("indexq")
        };

        public IDictionary<(QueueType, QueuePriority), QueueConfig> GetAll() => _queues;

        public QueueConfig GetByQueue(
            QueueType queue) => GetByQueue(queue, QueuePriority.Normal);

        public QueueConfig GetByQueue(
            QueueType queue,
            QueuePriority priority) => _queues[(queue, priority)];
    }
}