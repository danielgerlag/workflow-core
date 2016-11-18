using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Interface
{
    /// <remarks>
    /// The implemention of this interface will be responsible for
    /// providing a (distributed) locking mechanism to manage in flight workflows    
    /// </remarks>
    public interface IDistributedLockProvider
    {
        Task<bool> AcquireLock(string Id);

        Task ReleaseLock(string Id);
    }
}
