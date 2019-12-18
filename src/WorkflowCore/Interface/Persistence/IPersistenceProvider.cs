using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IPersistenceProvider : IWorkflowRepository, ISubscriptionRepository, IEventRepository
    {        

        Task PersistErrors(IEnumerable<ExecutionError> errors);

        void EnsureStoreExists();

    }
}
