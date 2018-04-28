# Redis Relock DLM provider for Workflow Core

Provides [DLM](https://en.wikipedia.org/wiki/Distributed_lock_manager) support  on [Workflow Core](../../README.md) using [Redis Redlock](http://redis.io/topics/distlock).

This makes it possible to have a cluster of nodes processing your workflows, along with a queue provider.

## Installing

Install the NuGet package "WorkflowCore.LockProviders.Redlock"

```
PM> Install-Package WorkflowCore.LockProviders.Redlock
```

## Usage

Use the .UseRedlock extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseRedlock(new DnsEndPoint("host1", 6379), new DnsEndPoint("host2", 6379)));
```