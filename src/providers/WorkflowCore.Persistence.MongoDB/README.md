# MongoDB Persistence provider for Workflow Core

Provides support to persist workflows running on [Workflow Core](../../README.md) to a MongoDB database.

## Installing

Install the NuGet package "WorkflowCore.Persistence.MongoDB"

```
PM> Install-Package WorkflowCore.Persistence.MongoDB
```

## Usage

Use the .UseMongoDB extension method when building your service provider.

```C#
services.AddWorkflow(x => x.UseMongoDB(@"mongodb://localhost:27017", "workflow"));
```

### Configuring the ObjectSerializer

When using MongoDB persistence with user-defined data classes, you need to configure which types are allowed to be deserialized. This is done via the `serializerTypeFilter` parameter:

```C#
services.AddWorkflow(x => x.UseMongoDB(
    @"mongodb://localhost:27017", 
    "workflow",
    serializerTypeFilter: type =>
        MongoDB.Bson.Serialization.Serializers.ObjectSerializer.DefaultAllowedTypes(type) || 
        type.FullName?.StartsWith("MyApp.") == true));
```

This configuration allows:
- All default MongoDB allowed types (primitives, collections, etc.)
- Types in your application namespace (e.g., `MyApp.*`)

**Important:** You must configure the serializer to allow your workflow data types, otherwise you will encounter a `BsonSerializationException` when MongoDB tries to deserialize your data.

Example for multiple namespaces:

```C#
services.AddWorkflow(x => x.UseMongoDB(
    @"mongodb://localhost:27017", 
    "workflow",
    serializerTypeFilter: type =>
    {
        if (MongoDB.Bson.Serialization.Serializers.ObjectSerializer.DefaultAllowedTypes(type))
            return true;
            
        var fullName = type.FullName ?? "";
        return fullName.StartsWith("MyApp.") || 
               fullName.StartsWith("MyCompany.Models.") ||
               fullName.StartsWith("WorkflowCore.");
    }));
```

### State object serialization

By default (to maintain backwards compatibility), the state object is serialized using a two step serialization process using object -> JSON -> BSON serialization.
This approach has some limitations, for example you cannot control which types will be used in MongoDB for particular fields and you cannot use basic types that are not present in JSON (decimal, timestamp, etc).

To eliminate these limitations, you can use a direct object -> BSON serialization and utilize all serialization possibilities that MongoDb driver provides. You can read more in the [MongoDb CSharp documentation](https://mongodb.github.io/mongo-csharp-driver/1.11/serialization/).
To enable direct serialization you need to register a class map for you state class somewhere in your startup process before you run `WorkflowHost`.

```C#
private void RunWorkflow()
{
    var host = this._serviceProvider.GetService<IWorkflowHost>();
    if (host == null)
    {
        return;
    }

    if (!BsonClassMap.IsClassMapRegistered(typeof(MyWorkflowState)))
    {
        BsonClassMap.RegisterClassMap<MyWorkflowState>(cm =>
        {
            cm.AutoMap();
        });
    }

    host.RegisterWorkflow<MyWorkflow, MyWorkflowState>();

    host.Start();
}

```
