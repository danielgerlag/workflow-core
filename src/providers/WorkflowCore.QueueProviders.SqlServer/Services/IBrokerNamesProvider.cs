#region using

using System;
using System.Linq;
using WorkflowCore.Interface;

#endregion

namespace WorkflowCore.QueueProviders.SqlServer.Services
{
    /// <summary>
    ///     Base interface for <see cref="BrokerNamesProvider" />
    /// </summary>
    public interface IBrokerNamesProvider
    {
        BrokerNames GetByQueue(QueueType queue);
    }
}