# AWS providers for Workflow Core

* Provides Queueing support  on [Workflow Core](../../README.md) using AWS Simple Queue Service.

This makes it possible to have a cluster of nodes processing your workflows.

## Installing

Install the NuGet package "WorkflowCore.Providers.AWS"

```
PM> Install-Package WorkflowCore.Providers.AWS
```

## Usage

Use the .UseAwsSimpleQueueService extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseAwsSimpleQueueService(awsCredentials, amazonSQSConfig));
```