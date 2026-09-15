# Redis providers

The `WorkflowCore.Providers.Redis` package provides persistence, queues, distributed locking, and an event hub backed by Redis. This makes it possible to run a cluster of nodes processing your workflows.

See [multi-node clusters](multi-node-clusters.md) for how queues and locks fit a multi-node setup.

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

## Configuration

Use the `IServiceCollection` extension methods when building your service provider:

* `.UseRedisPersistence`
* `.UseRedisQueues`
* `.UseRedisLocking`
* `.UseRedisEventHub`

```csharp
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

## Queue storage (List vs SortedSet)

`UseRedisQueues(connection, prefix)` defaults to `RedisQueueStorage.List`. Opt in to a sorted set with `UseRedisQueues(connection, prefix, RedisQueueStorage.SortedSet)`.

Both modes use the same keys: `{prefix}-workflows`, `{prefix}-events`, and `{prefix}-index`.

| Storage | Redis type | Uniqueness | When to use |
|---|---|---|---|
| `List` (**default**) | LIST | Atomic Lua `LINSERT` / `RPUSH` / `LREM` | Existing LIST deployments; rolling-upgrade friendly |
| `SortedSet` | ZSET | `ZADD NX` + Redis `TIME` scores; dequeue is Lua `ZRANGE` + `ZREM` | Opt-in unique sorted set. `Start()` migrates leftover LIST items in place |

**Do not mix `List` and `SortedSet` on the same key prefix.** Redis cannot store both types on one key (`WRONGTYPE`), and mixed hosts will split or lose work. Pick one implementation for a prefix and use it on every host.

`SortedSet` requires a coordinated cutover: stop every host using the prefix, deploy with `RedisQueueStorage.SortedSet`, then start. `Start()` migrates leftover LIST items (first occurrence kept).

See the [Redis provider README](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.Redis) for full detail.
