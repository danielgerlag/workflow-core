# AWS providers for Workflow Core

* Provides Queueing support  on [Workflow Core](../../README.md) using AWS Simple Queue Service.
* Provides Distributed locking support  on [Workflow Core](../../README.md) using DynamoDB.

This makes it possible to have a cluster of nodes processing your workflows.

## Installing

Install the NuGet package "WorkflowCore.Providers.AWS"

```
PM> Install-Package WorkflowCore.Providers.AWS
```

## Usage

Use the `.UseAwsSimpleQueueService` and `.UseAwsDynamoLocking` extension methods when building your service provider.

```C#
services.AddWorkflow(cfg =>
{
    cfg.UseAwsSimpleQueueService(new EnvironmentVariablesAWSCredentials(), new AmazonSQSConfig() { RegionEndpoint = RegionEndpoint.USWest2 });
    cfg.UseAwsDynamoLocking(new EnvironmentVariablesAWSCredentials(), new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.USWest2 }, "workflow-core-locks");
});
```