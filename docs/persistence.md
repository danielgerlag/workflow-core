# Persistence

Since workflows are typically long running processes, they will need to be persisted to storage between steps.
There are several persistence providers available as separate Nuget packages.

* MemoryPersistenceProvider *(Default provider, for demo and testing purposes)*
* [MongoDB](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Persistence.MongoDB)
* [SQL Server](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Persistence.SqlServer)
* [PostgreSQL](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Persistence.PostgreSQL)
* [Sqlite](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Persistence.Sqlite)
* [Amazon DynamoDB](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.AWS)
* [Cosmos DB](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.Azure)
* [Azure Table Storage](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.Azure)
* [Redis](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.Redis)
* [Oracle](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Persistence.Oracle)

## Restricting deserialized types (Entity Framework providers)

The Entity Framework based providers (SQL Server, PostgreSQL, MySQL, Sqlite, Oracle) store workflow data as JSON with `TypeNameHandling.All`, so the type of each object is read from the stored data.
As a defense-in-depth measure, you can supply a `SerializationBinder` that only allows the types your workflows use, so that a tampered row cannot instantiate arbitrary types.

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework.Services;
using WorkflowCore.Persistence.Sqlite;

public class MyData
{
    public List<string> Tags { get; set; }
}

public class AllowListBinder : DefaultSerializationBinder
{
    // MyData.Tags is stored with its own type name, so List<string> must be allowed too
    private static readonly HashSet<Type> Allowed = new HashSet<Type> { typeof(MyData), typeof(List<string>) };

    public override Type BindToType(string assemblyName, string typeName)
    {
        var type = base.BindToType(assemblyName, typeName);
        if (!Allowed.Contains(type) && type.Assembly != typeof(IWorkflowHost).Assembly)
            throw new JsonSerializationException($"Type '{typeName}' is not allowed.");

        return type;
    }
}

var binder = new AllowListBinder();

services.AddWorkflow(x =>
{
    x.UseSqlite(@"Data Source=database.db;", true);
    x.UsePersistence(sp => new EntityFrameworkPersistenceProvider(new SqliteContextFactory(@"Data Source=database.db;"), true, false,
        settings => settings.SerializationBinder = binder));
});
```

Persistence providers are registered as transient, so the configure action runs every time a provider is resolved.
Create the binder once, outside the lambda, so its type cache is reused; the shared instance must be thread-safe.

The binder must allow every non-primitive type that is persisted, including collection types such as `List<T>` and their generic arguments, as well as any types stored in step outcomes, event data or extension attributes.
Workflow Core's own types (e.g. step persistence data) live in the `WorkflowCore` assembly, which the example above allows.
Changing `TypeNameHandling` or other settings that affect the JSON format may make existing persisted data unreadable.

## Implementing a custom persistence provider

To implement a custom persistence provider, create a class that implements `IPersistenceProvider` interface:

```csharp
public interface IPersistenceProvider : IWorkflowRepository, ISubscriptionRepository, IEventRepository, IScheduledCommandRepository
{        
    Task PersistErrors(IEnumerable<ExecutionError> errors, CancellationToken cancellationToken = default);
    void EnsureStoreExists();
}
```

The `IPersistenceProvider` interface combines four repository interfaces:

### IWorkflowRepository
Handles workflow instance storage and retrieval:
- `CreateNewWorkflow` - Create and store a new workflow instance
- `PersistWorkflow` - Update an existing workflow instance
- `GetWorkflowInstance` - Retrieve a specific workflow instance
- `GetRunnableInstances` - Get workflow instances ready for execution

### IEventRepository  
Manages workflow events:
- `CreateEvent` - Store a new event
- `GetEvent` - Retrieve a specific event
- `GetRunnableEvents` - Get events ready for processing
- `MarkEventProcessed/Unprocessed` - Update event status

### ISubscriptionRepository
Handles event subscriptions:
- `CreateEventSubscription` - Create new event subscription
- `GetSubscriptions` - Query subscriptions for events
- `TerminateSubscription` - Remove a subscription
- `SetSubscriptionToken/ClearSubscriptionToken` - Manage subscription locking

### IScheduledCommandRepository
For future command scheduling (optional):
- `ScheduleCommand` - Schedule a command for future execution
- `ProcessCommands` - Execute scheduled commands
- `SupportsScheduledCommands` - Indicates if provider supports this feature

Once implemented, register your provider:

```csharp
services.AddWorkflow(options => 
{
    options.UsePersistence(sp => new MyCustomPersistenceProvider());
});
```