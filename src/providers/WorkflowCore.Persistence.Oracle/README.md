# Oracle Persistence provider for Workflow Core

Provides support to persist workflows running on [Workflow Core](../../README.md) to an Oracle database.

## Installing

Install the NuGet package "WorkflowCore.Persistence.Oracle"

```
PM> Install-Package WorkflowCore.Persistence.Oracle -Pre
```

## Usage

Use the .UseOracle extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseOracle(@"Server=127.0.0.1;Database=workflow;User=root;Password=password;", true, true));
```

You can also add specific database version compatibility if needed.

```C#
services.AddWorkflow(x =>
    {
        x.UseOracle(connectionString, false, true, options =>
            {
                options.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
            });
    });
```