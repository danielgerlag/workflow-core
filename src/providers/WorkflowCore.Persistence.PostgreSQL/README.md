# PostgreSQL Persistence provider for Workflow Core

Provides support to persist workflows running on [Workflow Core](../../README.md) to a PostgreSQL database.

## Installing

Install the NuGet package "WorkflowCore.Persistence.PostgreSQL"

```
PM> Install-Package WorkflowCore.Persistence.PostgreSQL -Pre
```

## Usage

Use the .UsePostgreSQL extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UsePostgreSQL(@"Server=127.0.0.1;Port=5432;Database=workflow;User Id=postgres;Password=password;", true, true));
```
