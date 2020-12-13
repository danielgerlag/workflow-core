using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    public interface IQueueCache
    {
        Task Add(string id);
        Task Remove(string id);
        Task<bool> Contains(string id);
    }
}