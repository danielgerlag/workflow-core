# SQL Server Persistence provider for Workflow Core

Provides support to persist workflows running on [Workflow Core](../../../README.md) to a SQL Server database.

## Installing

Install the NuGet package "WorkflowCore.Persistence.SqlServer"

```
PM> Install-Package WorkflowCore.Persistence.SqlServer
```

## Usage

Use the .UseSqlServer extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseSqlServer(@"Server=.;Database=WorkflowCore;Trusted_Connection=True;", true, true));
```
