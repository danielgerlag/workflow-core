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