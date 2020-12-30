
# RavenDB Persistence provider for Workflow Core

Provides support to persist workflows running on [Workflow Core](../../README.md) to a RavenDB database.

## Installing

Install the NuGet package "WorkflowCore.Persistence.RavenDB"

```
PM> Install-Package WorkflowCore.Persistence.RavenDB
```

## Usage

Compose your RavenStoreOptions using the model provided by the library. 

```C#
var options = new RavenStoreOptions {
	ServerUrl = "https://ravendbserver.domain.com:8080",
	DatabaseName = "TestDatabase",
	CertificatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources/servercert.pfx"),
	CertificatePassword = "CertificatePassword"
}
```

Use the `.UseRavenDB` extension method when building your service provider, passing in the options you configured for the RavenDB store. 

```C#
services.AddWorkflow(x => x.UseRavenDB(options));
```
