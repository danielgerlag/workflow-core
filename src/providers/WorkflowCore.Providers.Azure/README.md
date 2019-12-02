# Azure providers for Workflow Core

* Provides [DLM](https://en.wikipedia.org/wiki/Distributed_lock_manager) support  on [Workflow Core](../../README.md) using Azure Blob Storage leases.
* Provides Queueing support  on [Workflow Core](../../README.md) using Azure Storage queues.
* Provides event hub support on [Workflow Core](../../README.md) backed by Azure Service Bus.

This makes it possible to have a cluster of nodes processing your workflows.

## Installing

Install the NuGet package "WorkflowCore.Providers.Azure"

Using Nuget package console
```
PM> Install-Package WorkflowCore.Providers.Azure
```
Using .NET CLI
```
dotnet add package WorkflowCore.Providers.Azure
```

## Usage

Use the `IServiceCollection` extension methods when building your service provider
* .UseAzureSynchronization
* .UseAzureServiceBusEventHub

```C#
services.AddWorkflow(options => 
{
	options.UseAzureSynchronization("azure storage connection string");
	options.UseAzureServiceBusEventHub("service bus connection string", "topic name", "subscription name");
});
```