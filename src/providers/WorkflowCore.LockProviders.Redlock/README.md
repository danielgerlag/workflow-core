# Redis Relock DLM provider for Workflow Core

Provides [DLM](https://en.wikipedia.org/wiki/Distributed_lock_manager) support  on [Workflow Core](../../README.md) using [Redis Redlock](http://redis.io/topics/distlock).

This makes it possible to have a cluster of nodes processing your workflows, along with a queue provider.

## Installing

Install the NuGet package "WorkflowCore.LockProviders.Redlock"

```
PM> Install-Package WorkflowCore.LockProviders.Redlock -Pre
```

## Usage

Use the .UseRedlock extension method when building your service provider.

```C#
redis = ConnectionMultiplexer.Connect("127.0.0.1");
services.AddWorkflow(x => x.UseRedlock(redis));
```

*Adapted from https://github.com/KidFashion/redlock-cs*
