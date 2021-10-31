using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IScheduledCommandRepository
    {
        bool SupportsScheduledCommands { get; }

        Task ScheduleCommand(ScheduledCommand command);

        Task ProcessCommands(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default);
    }
}
