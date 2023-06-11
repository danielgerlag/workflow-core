using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    public interface IActivityTaskProvider
    {
        Task WaitForActivityCreation(string activity, string workflowInstanceId, CancellationToken cancellationToken = default);
    }
}