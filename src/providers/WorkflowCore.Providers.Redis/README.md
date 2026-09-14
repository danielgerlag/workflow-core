# Redis providers for Workflow Core

* Provides Persistence support on [Workflow Core](../../README.md) backed by Redis.
* Provides Queueing support on [Workflow Core](../../README.md) backed by Redis.
* Provides Distributed locking support on [Workflow Core](../../README.md) backed by Redis.
* Provides event hub support on [Workflow Core](../../README.md) backed by Redis.

This makes it possible to have a cluster of nodes processing your workflows.

## Installing

Install the NuGet package "WorkflowCore.Providers.Redis"

Using Nuget package console
```
PM> Install-Package WorkflowCore.Providers.Redis
```
Using .NET CLI
```
dotnet add package WorkflowCore.Providers.Redis
```


## Usage

Use the `IServiceCollection` extension methods when building your service provider
* .UseRedisPersistence
* .UseRedisQueues
* .UseRedisLocking
* .UseRedisEventHub

```C#
using WorkflowCore.Providers.Redis.Services;

services.AddWorkflow(cfg =>
{
    cfg.UseRedisPersistence("localhost:6379", "app-name");
    cfg.UseRedisLocking("localhost:6379");
    cfg.UseRedisQueues("localhost:6379", "app-name"); // LIST (default)
    // cfg.UseRedisQueues("localhost:6379", "app-name", RedisQueueStorage.SortedSet);
    cfg.UseRedisEventHub("localhost:6379", "channel-name");
});
```

`UseRedisQueues` stores pending work on `{prefix}-workflows`, `{prefix}-events`, and `{prefix}-index`. Choose the Redis value type with `RedisQueueStorage`:

| Storage | Redis type | Uniqueness | When to use |
|---|---|---|---|
| `List` (**default**) | LIST | Atomic Lua `LINSERT` / `RPUSH` / `LREM` | Existing LIST deployments; rolling upgrades with older LIST hosts |
| `SortedSet` | ZSET | `ZADD NX` + Redis `TIME` scores; dequeue is Lua `ZRANGE` + `ZREM` | Opt-in unique sorted set. `Start()` migrates leftover LIST items in place |

**Do not mix `List` and `SortedSet` on the same key prefix.** Redis cannot store both types on one key (`WRONGTYPE`), and mixed hosts will split or lose work. Pick one implementation for a prefix and use it on every host.

`List` is the default so existing deployments keep working without a key-type cutover. `SortedSet` is a coordinated opt-in: stop every host using the prefix, deploy with `RedisQueueStorage.SortedSet`, then start. `Start()` migrates leftover LIST items (first occurrence kept). Dequeue in `SortedSet` mode does not require Redis 5+ (no native `ZPOPMIN`).
