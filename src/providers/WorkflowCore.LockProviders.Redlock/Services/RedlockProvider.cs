using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using RedLockNet;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.LockProviders.Redlock.Services
{
    public class RedlockProvider : IDistributedLockProvider, IDisposable
    {
        private readonly RedLockFactory _redlockFactory;
        private readonly TimeSpan _lockTimeout = TimeSpan.FromMinutes(10);
        private readonly List<IRedLock> ManagedLocks = new List<IRedLock>();

        public RedlockProvider(params DnsEndPoint[] endpoints)
        {
            var redlockEndpoints = new List<RedLockEndPoint>();

            foreach (var ep in endpoints)
                redlockEndpoints.Add(ep);
            

            _redlockFactory = RedLockFactory.Create(redlockEndpoints);

        }

        public async Task<bool> AcquireLock(string Id, CancellationToken cancellationToken)
        {
            
            var redLock = await _redlockFactory.CreateLockAsync(Id, _lockTimeout);

            if (redLock.IsAcquired)
            {
                lock (ManagedLocks)
                {
                    ManagedLocks.Add(redLock);
                }
                return true;
            }

            return false;
        }



        public Task ReleaseLock(string Id)
        {
            lock (ManagedLocks)
            {
                foreach (var redLock in ManagedLocks)
                {
                    if (redLock.Resource == Id)
                    {
                        redLock.Dispose();
                        ManagedLocks.Remove(redLock);
                        break;
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _redlockFactory?.Dispose();
        }

    }
}