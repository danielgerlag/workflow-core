#region using

using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.SqlServer.Models;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Interfaces
{    
    public interface IQueueConfigProvider
    {
        IDictionary<(QueueType, QueuePriority), QueueConfig> GetAll();
        QueueConfig GetByQueue(QueueType queue);
        QueueConfig GetByQueue(QueueType queue, QueuePriority priority);
    }
}