# Sqlite Persistence provider for Workflow Core

Provides support to persist workflows running on [Workflow Core](../../README.md) to a Sqlite database.

## Installing

Install the NuGet package "WorkflowCore.Persistence.Sqlite"

```
PM> Install-Package WorkflowCore.Persistence.Sqlite -Pre
```

## Usage

Use the .UseSqlite extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseSqlite(@"Data Source=database.db;", true));
```
