using Microsoft.Extensions.Logging;
using org.apache.zookeeper;
using org.apache.zookeeper.recipes.@lock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.LockProviders.ZooKeeper.Services
{
    public class ZooKeeperLockProvider : IDistributedLockProvider
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;
        private Dictionary<int, org.apache.zookeeper.ZooKeeper> _sessions = new Dictionary<int, org.apache.zookeeper.ZooKeeper>();
        private List<WriteLock> _managedLocks = new List<WriteLock>();

        public ZooKeeperLockProvider(string connectionString, ILoggerFactory loggerFactory)
        {
            throw new NotImplementedException();

            _logger = loggerFactory.CreateLogger<ZooKeeperLockProvider>();
            _connectionString = connectionString;
            WriteLock wl = new WriteLock(GetSession(), @"/workflow_locks", null);
            //try
            //{
            //    //todo: clean this up
            //    //wl.Lock().Wait();
            //}
            //catch
            //{
            //}
        }

        public async Task<bool> AcquireLock(string Id)
        {
            try
            {
                lock (_managedLocks)
                {
                    WriteLock wl = new WriteLock(GetSession(), @"/workflow_locks/" + Id, null);
                    if (wl.Lock().Result)
                    {
                        _managedLocks.Add(wl);
                        return true;
                    }
                    else
                    {
                        wl.unlock();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                //log
                return false;
            }
        }

        public async Task ReleaseLock(string Id)
        {
            lock (_managedLocks)
            {
                string lockId = @"/workflow_locks/" + Id;
                var list = _managedLocks.Where(x => x.Dir == lockId).ToList();
                foreach (var item in list)
                {
                    item.unlock().Wait();
                    _managedLocks.Remove(item);
                }
            }
        }

        private org.apache.zookeeper.ZooKeeper GetSession()
        {
            var session = new org.apache.zookeeper.ZooKeeper(_connectionString, 30000, new StubWatcher());
            return session;

            //if (!_sessions.ContainsKey(System.Threading.Thread.CurrentThread.ManagedThreadId))
            //{
            //    var session = new org.apache.zookeeper.ZooKeeper(_connectionString, 60000, new StubWatcher());
            //    _sessions.Add(System.Threading.Thread.CurrentThread.ManagedThreadId, session);
            //}
            //return _sessions[System.Threading.Thread.CurrentThread.ManagedThreadId];
        }

        ~ZooKeeperLockProvider()
        {            
        }

        class StubWatcher : Watcher
        {
            public override async Task process(WatchedEvent @event)
            {                
            }
        }
    }
}
