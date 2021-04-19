using Microsoft.Extensions.Caching.Distributed;

namespace WorkflowCore.Interface
{
    public interface IGreyList : IDistributedCache
    {
    }
}