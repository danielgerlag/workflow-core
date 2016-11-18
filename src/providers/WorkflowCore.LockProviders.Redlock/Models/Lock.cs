/*
 * Adapted from https://github.com/KidFashion/redlock-cs 
 */
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.LockProviders.Redlock.Models
{
    public class Lock
    {

        public Lock(RedisKey resource, RedisValue val, TimeSpan validity)
        {
            this.resource = resource;
            this.val = val;
            this.validity_time = validity;
        }

        private RedisKey resource;

        private RedisValue val;

        private TimeSpan validity_time;

        public RedisKey Resource { get { return resource; } }

        public RedisValue Value { get { return val; } }

        public TimeSpan Validity { get { return validity_time; } }
    }
}
