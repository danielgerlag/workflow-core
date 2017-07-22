/*
 * Adapted from https://github.com/KidFashion/redlock-cs 
 */
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.LockProviders.Redlock.Models;

namespace WorkflowCore.LockProviders.Redlock.Services
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public class RedlockProvider : IDistributedLockProvider
    {
        const int DefaultRetryCount = 3;
        readonly TimeSpan DefaultRetryDelay = new TimeSpan(0, 0, 0, 0, 200);
        const double ClockDriveFactor = 0.01;

        protected int Quorum { get { return (redisMasterDictionary.Count / 2) + 1; } }

        const String UnlockScript = @"
            if redis.call(""get"",KEYS[1]) == ARGV[1] then
                return redis.call(""del"",KEYS[1])
            else
                return 0
            end";

        private static List<Lock> OwnLocks = new List<Lock>();

        public RedlockProvider(params IConnectionMultiplexer[] list)
        {
            foreach (var item in list)
                this.redisMasterDictionary.Add(item.GetEndPoints().First().ToString(), item);
        }

        public async Task<bool> AcquireLock(string Id, CancellationToken cancellationToken)
        {
            Lock lockObject = null;
            if (Lock(Id, TimeSpan.FromMinutes(30), out lockObject))
            {
                OwnLocks.Add(lockObject);
                return true;
            }
            return false;
        }

        public async Task ReleaseLock(string Id)
        {
            var list = OwnLocks.Where(x => x.Resource == Id).ToList();
            foreach (var lockObject in list)
            {
                Unlock(lockObject);
                OwnLocks.Remove(lockObject);
            }
        }

        protected static byte[] CreateUniqueLockId()
        {
            return Guid.NewGuid().ToByteArray();
        }


        protected Dictionary<String, IConnectionMultiplexer> redisMasterDictionary = new Dictionary<string, IConnectionMultiplexer>();

        //TODO: Refactor passing a ConnectionMultiplexer
        protected bool LockInstance(string redisServer, string resource, byte[] val, TimeSpan ttl)
        {

            bool succeeded;
            try
            {
                var redis = this.redisMasterDictionary[redisServer];
                succeeded = redis.GetDatabase().StringSet(resource, val, ttl, When.NotExists);
            }
            catch (Exception)
            {
                succeeded = false;
            }
            return succeeded;
        }

        //TODO: Refactor passing a ConnectionMultiplexer
        protected void UnlockInstance(string redisServer, string resource, byte[] val)
        {
            RedisKey[] key = { resource };
            RedisValue[] values = { val };
            var redis = redisMasterDictionary[redisServer];
            redis.GetDatabase().ScriptEvaluate(
                UnlockScript,
                key,
                values
                );
        }

        protected bool Lock(RedisKey resource, TimeSpan ttl, out Lock lockObject)
        {
            var val = CreateUniqueLockId();
            Lock innerLock = null;
            bool successfull = retry(DefaultRetryCount, DefaultRetryDelay, () =>
            {
                try
                {
                    int n = 0;
                    var startTime = DateTime.Now;

                    // Use keys
                    for_each_redis_registered(
                        redis =>
                        {
                            if (LockInstance(redis, resource, val, ttl)) n += 1;
                        }
                    );

                    /*
                     * Add 2 milliseconds to the drift to account for Redis expires
                     * precision, which is 1 millisecond, plus 1 millisecond min drift 
                     * for small TTLs.        
                     */
                    var drift = Convert.ToInt32((ttl.TotalMilliseconds * ClockDriveFactor) + 2);
                    var validity_time = ttl - (DateTime.Now - startTime) - new TimeSpan(0, 0, 0, 0, drift);

                    if (n >= Quorum && validity_time.TotalMilliseconds > 0)
                    {
                        innerLock = new Lock(resource, val, validity_time);
                        return true;
                    }
                    else
                    {
                        for_each_redis_registered(
                            redis =>
                            {
                                UnlockInstance(redis, resource, val);
                            }
                        );
                        return false;
                    }
                }
                catch (Exception)
                { return false; }
            });

            lockObject = innerLock;
            return successfull;
        }

        protected void for_each_redis_registered(Action<IConnectionMultiplexer> action)
        {
            foreach (var item in redisMasterDictionary)
            {
                action(item.Value);
            }
        }

        protected void for_each_redis_registered(Action<String> action)
        {
            foreach (var item in redisMasterDictionary)
            {
                action(item.Key);
            }
        }

        protected bool retry(int retryCount, TimeSpan retryDelay, Func<bool> action)
        {
            int maxRetryDelay = (int)retryDelay.TotalMilliseconds;
            Random rnd = new Random();
            int currentRetry = 0;

            while (currentRetry++ < retryCount)
            {
                if (action()) return true;
                Thread.Sleep(rnd.Next(maxRetryDelay));
            }
            return false;
        }

        protected void Unlock(Lock lockObject)
        {
            for_each_redis_registered(redis =>
            {
                UnlockInstance(redis, lockObject.Resource, lockObject.Value);
            });
        }

        public async Task Start()
        {

        }

        public async Task Stop()
        {

        }

    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
