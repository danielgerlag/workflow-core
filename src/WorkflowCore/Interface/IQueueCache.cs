using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IQueueCache
    {
        /// <summary>Adds the specified element to the cache or update if is expired.</summary>
        /// <returns>true if the element is added or updated; false if the element is already present.</returns>
        Task<bool> AddOrUpdateAsync(
            CacheItem item, 
            CancellationToken cancellationToken);

        /// <summary>Remove specified element from the cache.</summary>
        Task RemoveAsync(
            CacheItem item, 
            CancellationToken cancellationToken);
    }
}