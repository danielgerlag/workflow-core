# ZeroMQ distributed lock manager for Workflow Core *(experimental)*

Provides distributed locking without a central server on [Workflow Core](../../README.md) using [ZeroMQ](http://zeromq.org/).

This makes it possible to have a cluster of nodes processing your workflows, along with a queue provider.

## Installing

Install the NuGet package "WorkflowCore.LockProviders.ZeroMQ"

```
PM> Install-Package WorkflowCore.LockProviders.ZeroMQ -Pre
```

## Usage

Use the .UseZeroMQLocking extension method when building your service provider.

```C#
var peers = new List<string>();
peers.Add("machine2:5551");
peers.Add("machine3:5551");
services.AddWorkflow(x => x.UseZeroMQLocking(5551, peers));

```
## Experimental package

This package is still experimental and not recommended for production use.