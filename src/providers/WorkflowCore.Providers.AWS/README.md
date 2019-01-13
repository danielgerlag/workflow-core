# AWS providers for Workflow Core

* Provides persistence for [Workflow Core](../../README.md) using DynamoDB.
* Provides Queueing support on [Workflow Core](../../README.md) using AWS Simple Queue Service.
* Provides Distributed locking support on [Workflow Core](../../README.md) using DynamoDB.

This makes it possible to have a cluster of nodes processing your workflows.

## Installing

Install the NuGet package "WorkflowCore.Providers.AWS"

```
PM> Install-Package WorkflowCore.Providers.AWS
```

## Usage

Use the `IServiceCollection` extension methods when building your service provider
* .UseAwsDynamoPersistence
* .UseAwsSimpleQueueService
* .UseAwsDynamoLocking

```C#
services.AddWorkflow(cfg =>
{
    cfg.UseAwsDynamoPersistence(new EnvironmentVariablesAWSCredentials(), new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.USWest2 }, "table-prefix");
    cfg.UseAwsDynamoLocking(new EnvironmentVariablesAWSCredentials(), new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.USWest2 }, "workflow-core-locks");
    cfg.UseAwsSimpleQueueService(new EnvironmentVariablesAWSCredentials(), new AmazonSQSConfig() { RegionEndpoint = RegionEndpoint.USWest2 });    
});
```

If any AWS resources do not exists, they will be automatcially created. By default, all DynamoDB tables and indexes will be provisioned with a throughput of 1, you can modify these values from the AWS console.
You may also specify a prefix for the dynamo table names.