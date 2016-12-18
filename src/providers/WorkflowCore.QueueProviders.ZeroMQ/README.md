# ZeroMQ queue provider for Workflow Core *(experimental)*

Provides distributed worker support without the need for a message broker on [Workflow Core](../../README.md) using [ZeroMQ](http://zeromq.org/).

This makes it possible to have a cluster of nodes processing your workflows, along with a distributed lock manager.

## Installing

Install the NuGet package "WorkflowCore.QueueProviders.ZeroMQ"

```
PM> Install-Package WorkflowCore.QueueProviders.ZeroMQ -Pre
```

## Usage

Use the .UseZeroMQQueuing extension method when building your service provider.

```C#
var peers = new List<string>();
peers.Add("machine2:5557");
peers.Add("machine3:5557");
services.AddWorkflow(x => x.UseZeroMQQueuing(5557, peers));

```
## Experimental package

This package is still experimental and not recommended for production use.