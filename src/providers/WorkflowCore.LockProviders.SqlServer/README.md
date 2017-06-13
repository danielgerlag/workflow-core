# SQL Server DLM provider for Workflow Core

Provides [DLM](https://en.wikipedia.org/wiki/Distributed_lock_manager) support  on [Workflow Core](../../README.md) using SQL Server's sp_getapplock.

This makes it possible to have a cluster of nodes processing your workflows, along with a queue provider.

## Installing

Install the NuGet package "WorkflowCore.LockProviders.SqlServer"

```
PM> Install-Package WorkflowCore.LockProviders.SqlServer
```

## Usage

Use the .UseSqlServerLocking extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseSqlServerLocking("connection string"));
