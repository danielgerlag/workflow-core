# MySQL Persistence provider for Workflow Core

Provides support to persist workflows running on [Workflow Core](../../README.md) to a MySQL database.

## Installing

Install the NuGet package "WorkflowCore.Persistence.MySQL"

```
PM> Install-Package WorkflowCore.Persistence.MySQL -Pre
```

## Usage

Use the .UseMySQL extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseMySQL(@"Server=127.0.0.1;Database=workflow;User=root;Password=password;", true, true));
```
