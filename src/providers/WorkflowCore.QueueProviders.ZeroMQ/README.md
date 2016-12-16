# ZeroMQ provider for Workflow Core

Provides distributed worker support  on [Workflow Core](../../README.md) using [ZeroMQ](http://zeromq.org/).

This makes it possible to have a cluster of nodes processing your workflows, along with a distributed lock manager.

## Installing

Install the NuGet package "WorkflowCore.QueueProviders.ZeroMQ"

```
PM> Install-Package WorkflowCore.QueueProviders.ZeroMQ -Pre
```

## Usage

Use the .UseZeroMQ extension method when building your service provider.

```C#
var peers = new List<string>();
peers.Add("machine2:5557");
peers.Add("machine3:5557");
services.AddWorkflow(x => x.UseZeroMQQueuing(5557, peers));

```
