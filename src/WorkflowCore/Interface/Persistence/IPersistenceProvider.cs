using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IPersistenceProvider : IWorkflowRepository, ISubscriptionRepository, IEventRepository, IScheduledCommandRepository
    {        

        Task PersistErrors(IEnumerable<ExecutionError> errors, CancellationToken cancellationToken = default);

        void EnsureStoreExists();

    }
}
