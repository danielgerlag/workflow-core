# MySQL DLM provider for Workflow Core

Provides [DLM](https://en.wikipedia.org/wiki/Distributed_lock_manager) support  on [Workflow Core](../../README.md) using MySQL's GET_LOCK and RELEASE_LOCK.

This makes it possible to have a cluster of nodes processing your workflows, along with a queue provider.

## Installing

Install the NuGet package "WorkflowCore.LockProviders.MySQL"

```
PM> Install-Package WorkflowCore.LockProviders.MySQL
```

## Usage

Use the .UseMySqlLocking extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseMySqlLocking("connection string"));
