using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    /// <summary>
    /// Single node in-memory implementation of IDistributedLockProvider
    /// </summary>
    public class SingleNodeLockProvider : IDistributedLockProvider
    {   
        private List<string> _locks = new List<string>();
     
        public async Task<bool> AcquireLock(string Id)
        {
            lock (_locks)
            {
                if (_locks.Contains(Id))
                    return false;

                _locks.Add(Id);
                return true;
            }
        }

        public async Task ReleaseLock(string Id)
        {
            lock (_locks)
            {
                _locks.Remove(Id);
            }
        }

        public void Start()
        {

        }

        public void Stop()
        {

        }

    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
