using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IQueueCache
    {
        /// <summary>Adds the specified element to the cache.</summary>
        /// <param name="id"></param>
        /// <returns>true if the element is added; false if the element is already present or expired.</returns>
        Task<bool> Add(CacheItem id);

        /// <summary>Remove specified element from the cache.</summary>
        Task Remove(CacheItem id);
    }
}