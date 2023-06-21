using System;
using System.Threading.Tasks;
using System.Threading;

namespace WorkflowCore.Interface
{
    public interface IEventsPurger
    {
        Task PurgeEvents(DateTime olderThan, CancellationToken cancellationToken = default);
    }
}
