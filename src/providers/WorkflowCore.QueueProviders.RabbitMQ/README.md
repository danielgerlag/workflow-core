# RabbitMQ provider for Workflow Core

Provides distributed worker support  on [Workflow Core](../../README.md) using [RabbitMQ](https://www.rabbitmq.com/).

This makes it possible to have a cluster of nodes processing your workflows, along with a distributed lock manager.

## Installing

Install the NuGet package "WorkflowCore.QueueProviders.RabbitMQ"

```
PM> Install-Package WorkflowCore.QueueProviders.RabbitMQ -Pre
```

## Usage

Use the .UseRabbitMQ extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseRabbitMQ(new ConnectionFactory() { HostName = "localhost" });

```
