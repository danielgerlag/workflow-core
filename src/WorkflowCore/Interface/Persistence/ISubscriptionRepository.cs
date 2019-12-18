using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface ISubscriptionRepository
    {        
        Task<string> CreateEventSubscription(EventSubscription subscription);

        Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf);

        Task TerminateSubscription(string eventSubscriptionId);

        Task<EventSubscription> GetSubscription(string eventSubscriptionId);

        Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf);
        
        Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry);
        
        Task ClearSubscriptionToken(string eventSubscriptionId, string token);

    }
}
