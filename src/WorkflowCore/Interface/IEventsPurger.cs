using System;
using System.Threading.Tasks;
using System.Threading;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IEventsPurger
    {
        EventsPurgerOptions Options { get; }
        Task PurgeEvents(DateTime olderThan, CancellationToken cancellationToken = default);
    }
}
