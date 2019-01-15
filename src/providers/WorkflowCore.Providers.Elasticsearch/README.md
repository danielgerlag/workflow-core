# Elasticsearch plugin for Workflow Core

...

## Installing

Install the NuGet package "WorkflowCore.Providers.Elasticsearch"

Using Nuget package console
    ```
    PM> Install-Package WorkflowCore.Providers.Elasticsearch
    ```
Using .NET CLI
    ```
    dotnet add package WorkflowCore.Providers.Elasticsearch
    ```


## Configuration

Use the `.UseElasticsearch` extension method on `IServiceCollection` when building your service provider

```C#
using Nest;
...
services.AddWorkflow(cfg =>
{
	...
	cfg.UseElasticsearch(new ConnectionSettings(new Uri("http://localhost:9200")), "index_name");
});
```

## Usage

...