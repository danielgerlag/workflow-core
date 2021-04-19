using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace WorkflowCore.Services.BackgroundTasks
{
    public static class DistributedCacheExtensions
    {
        private static readonly byte[] _value = new byte[0];
        private static readonly TimeSpan _lifetime = TimeSpan.FromMinutes(5);

        public static async Task<bool> ContainsAsync(this IDistributedCache cache, string key)
        {
            return await cache.GetAsync(key) != null;
        }

        public static async Task SetAsync(this IDistributedCache cache, string key)
        {
            await cache.SetAsync(key, _value,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _lifetime
                });
        }
    }
}