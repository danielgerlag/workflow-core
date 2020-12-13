using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    public interface IQueueCache
    {
        /// <summary>Adds the specified element to the cache.</summary>
        /// <param name="id"></param>
        /// <returns>false if the element is added; true if the element is already present.</returns>
        Task<bool> ContainsOrAdd(string id);

        /// <summary>Remove specified element from the cache.</summary>
        Task Remove(string id);
    }
}