# SQL Server Service Broker queue provider for Workflow Core

Provides distributed worker support  on [Workflow Core](../../../README.md) using [SQL Server Service Broker](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-service-broker).

This makes it possible to have a cluster of nodes processing your workflows, along with a distributed lock manager.

## Installing

Install the NuGet package "WorkflowCore.QueueProviders.SqlServer"

```
PM> Install-Package WorkflowCore.QueueProviders.SqlServer -Pre
```

## Usage

Use the .UseSqlServerQueue extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseSqlServerQueue(sp => new SqlServerQueueProvider(connectionString, workflowHostName, canMigrateDB));

```

