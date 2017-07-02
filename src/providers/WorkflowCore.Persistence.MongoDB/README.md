# MongoDB Persistence provider for Workflow Core

Provides support to persist workflows running on [Workflow Core](../../README.md) to a MongoDB database.

## Installing

Install the NuGet package "WorkflowCore.Persistence.MongoDB"

```
PM> Install-Package WorkflowCore.Persistence.MongoDB
```

## Usage

Use the .UseMongoDB extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseMongoDB(@"mongodb://localhost:27017", "workflow"));
```
