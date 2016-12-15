using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.LockProviders.ZeroMQ.Services
{
    public class ZeroMQLockProvider : IDistributedLockProvider
    {
        public Task<bool> AcquireLock(string Id)
        {
            throw new NotImplementedException();
        }

        public Task ReleaseLock(string Id)
        {
            throw new NotImplementedException();
        }
    }
}
