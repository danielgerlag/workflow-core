using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IEventRepository
    {
        Task<string> CreateEvent(Event newEvent);

        Task<Event> GetEvent(string id);

        Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt);

        Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf);

        Task MarkEventProcessed(string id);

        Task MarkEventUnprocessed(string id);

    }
}
