# Azure providers for Workflow Core

* Provides [DLM](https://en.wikipedia.org/wiki/Distributed_lock_manager) support  on [Workflow Core](../../README.md) using Azure Blob Storage leases.
* Provides Queueing support  on [Workflow Core](../../README.md) using Azure Storage queues.

This makes it possible to have a cluster of nodes processing your workflows.

## Installing

Install the NuGet package "WorkflowCore.Providers.Azure"

```
PM> Install-Package WorkflowCore.Providers.Azure
```

## Usage

Use the .UseAzureSyncronization extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseAzureSyncronization("azure storage connection string"));
```