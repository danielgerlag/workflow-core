# AWS providers for Workflow Core

* Provides persistence for [Workflow Core](../../README.md) using DynamoDB.
* Provides Queueing support on [Workflow Core](../../README.md) using AWS Simple Queue Service.
* Provides Distributed locking support on [Workflow Core](../../README.md) using DynamoDB.
* Provides event hub support on [Workflow Core](../../README.md) backed by AWS Kinesis.

This makes it possible to have a cluster of nodes processing your workflows.

## Installing

Install the NuGet package "WorkflowCore.Providers.AWS"

```
PM> Install-Package WorkflowCore.Providers.AWS
```

## Usage (Persistence, Queueing and distributed locking)

Use the `IServiceCollection` extension methods when building your service provider
* .UseAwsDynamoPersistence
* .UseAwsSimpleQueueService
* .UseAwsDynamoLocking

```C#
services.AddWorkflow(cfg =>
{
    cfg.UseAwsDynamoPersistence(new EnvironmentVariablesAWSCredentials(), new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.USWest2 }, "table-prefix");
    cfg.UseAwsDynamoLocking(new EnvironmentVariablesAWSCredentials(), new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.USWest2 }, "workflow-core-locks");
    cfg.UseAwsSimpleQueueService(new EnvironmentVariablesAWSCredentials(), new AmazonSQSConfig() { RegionEndpoint = RegionEndpoint.USWest2 }, "queues-prefix");
});
```

If any AWS resources do not exists, they will be automatcially created. By default, all DynamoDB tables and indexes will be provisioned with a throughput of 1, you can modify these values from the AWS console.
You may also specify a prefix for the dynamo table names.

If you have a preconfigured dynamoClient, you can pass this in instead of the credentials and config
```C#
var client = new AmazonDynamoDBClient();
var sqsClient = new AmazonSQSClient();
services.AddWorkflow(cfg =>
{
    cfg.UseAwsDynamoPersistenceWithProvisionedClient(client, "table-prefix");
    cfg.UseAwsDynamoLockingWithProvisionedClient(client, "workflow-core-locks");
    cfg.UseAwsSimpleQueueServiceWithProvisionedClient(sqsClient, "queues-prefix");
});
```


## Usage (Kinesis)

Use the the `.UseAwsKinesis` extension method on `IServiceCollection` when building your service provider

```C#
services.AddWorkflow(cfg =>
{
    cfg.UseAwsKinesis(new EnvironmentVariablesAWSCredentials(), RegionEndpoint.USWest2, "app-name", "stream-name");
});
```
The Kinesis provider will also create a DynamoDB table to track the postion in each shard of the Kinesis stream.
A shard position will be tracked for each app name that you connect with.