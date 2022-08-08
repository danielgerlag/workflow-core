using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface ISubscriptionRepository
    {        
        Task<string> CreateEventSubscription(EventSubscription subscription);

        Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default);

        Task TerminateSubscription(string eventSubscriptionId);

        Task<EventSubscription> GetSubscription(string eventSubscriptionId, CancellationToken cancellationToken = default);

        Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default);
        
        Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry);
        
        Task ClearSubscriptionToken(string eventSubscriptionId, string token);

    }
}
