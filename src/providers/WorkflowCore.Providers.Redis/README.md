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
services.AddWorkflow(cfg =>
{
    cfg.UseRedisPersistence("localhost:6379", "app-name");
    cfg.UseRedisLocking("localhost:6379");
    cfg.UseRedisQueues("localhost:6379", "app-name");
    cfg.UseRedisEventHub("localhost:6379", "channel-name");
});
```
