using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkflowCore.Interface;

namespace WorkflowCore.Providers.Redis.Services
{
    public class RedisLockProvider : IDistributedLockProvider
    {
        private readonly ILogger _logger;
        private readonly string _keyName;
        private readonly string _connectionString;
        private readonly TimeSpan _leaseTime = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _heartbeat = TimeSpan.FromSeconds(15);
        private readonly List<string> _locks = new List<string>();
        private readonly AutoResetEvent _mutex = new AutoResetEvent(true);
        private Task _heartbeatTask;
        private CancellationTokenSource _cancellationTokenSource;
        private IConnectionMultiplexer _multiplexer;
        private IDatabase _redis;

        public RedisLockProvider(string connectionString, string keyName, ILoggerFactory logFactory)
        {
            _connectionString = connectionString;
            _keyName = keyName;
            _logger = logFactory.CreateLogger(GetType());
        }

        public async Task<bool> AcquireLock(string Id, CancellationToken cancellationToken)
        {
            if (_redis == null)
                throw new InvalidOperationException();

            if (await _redis.LockTakeAsync(_keyName, Id, _leaseTime))
            {
                _locks.Add(Id);
                return true;
            }

            return false;
        }

        public async Task ReleaseLock(string Id)
        {
            if (_redis == null)
                throw new InvalidOperationException();

            if (_mutex.WaitOne())
            {
                try
                {
                    _locks.Remove(Id);
                }
                finally
                {
                    _mutex.Set();
                }
            }

            await _redis.LockReleaseAsync(_keyName, Id);
        }

        public async Task Start()
        {
            if (_heartbeatTask != null)
                throw new InvalidOperationException();

            _multiplexer = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            _redis = _multiplexer.GetDatabase();

            _cancellationTokenSource = new CancellationTokenSource();

            _heartbeatTask = new Task(SendHeartbeat);
            _heartbeatTask.Start();
        }

        public async Task Stop()
        {
            _cancellationTokenSource.Cancel();
            _heartbeatTask.Wait();
            _heartbeatTask = null;

            await _multiplexer.CloseAsync();
            _redis = null;
            _multiplexer = null;
        }

        private async void SendHeartbeat()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_heartbeat, _cancellationTokenSource.Token);
                    if (_mutex.WaitOne())
                    {
                        try
                        {
                            foreach (var item in _locks)
                            {
                                _redis.LockExtend(_keyName, item, _leaseTime);
                            }
                        }
                        finally
                        {
                            _mutex.Set();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(default(EventId), ex, ex.Message);
                }
            }
        }
    }
}
