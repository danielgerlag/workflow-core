using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using WorkflowCore.Interface;

namespace WorkflowCore.Providers.Redis.Services
{
    public class RedisLockProvider : IDistributedLockProvider
    {
        private readonly ILogger _logger;        
        private readonly string _connectionString;
        private readonly string _prefix;
        private IConnectionMultiplexer _multiplexer;
        private RedLockFactory _redlockFactory;
        private readonly TimeSpan _lockTimeout = TimeSpan.FromMinutes(1);
        private readonly List<IRedLock> ManagedLocks = new List<IRedLock>();

        public RedisLockProvider(string connectionString, string prefix, ILoggerFactory logFactory)
        {
            _connectionString = connectionString;
            _prefix = prefix;
            _logger = logFactory.CreateLogger(GetType());
        }

        public async Task<bool> AcquireLock(string Id, CancellationToken cancellationToken)
        {
            if (_redlockFactory == null)
                throw new InvalidOperationException();

            var redLock = await _redlockFactory.CreateLockAsync(GetResource(Id), _lockTimeout);

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
            if (_redlockFactory == null)
                throw new InvalidOperationException();

            var resource = GetResource(Id);

            lock (ManagedLocks)
            {
                foreach (var redLock in ManagedLocks)
                {
                    if (redLock.Resource == resource)
                    {
                        redLock.Dispose();
                        ManagedLocks.Remove(redLock);
                        break;
                    }
                }
            }

            return Task.CompletedTask;
        }

        public async Task Start()
        {
            _multiplexer = await ConnectionMultiplexer.ConnectAsync(_connectionString);           
            _redlockFactory = RedLockFactory.Create(new List<RedLockMultiplexer> { new RedLockMultiplexer(_multiplexer) });
        }

        public async Task Stop()
        {
            _redlockFactory?.Dispose();
            await _multiplexer.CloseAsync();
            _multiplexer = null;

        }

        private string GetResource(string key)
        {
            if (string.IsNullOrEmpty(_prefix))
                return key;

            return $"{_prefix}:{key}";
        }
    }
}
