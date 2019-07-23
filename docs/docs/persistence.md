# Persistence

Since workflows are typically long running processes, they will need to be persisted to storage between steps.
There are several persistence providers available as separate Nuget packages.

* MemoryPersistenceProvider *(Default provider, for demo and testing purposes)*
* [MongoDB](../tree/master/src/providers/WorkflowCore.Persistence.MongoDB)
* [SQL Server](../tree/master/src/providers/WorkflowCore.Persistence.SqlServer)
* [PostgreSQL](../tree/master/src/providers/WorkflowCore.Persistence.PostgreSQL)
* [Sqlite](../tree/master/src/providers/WorkflowCore.Persistence.Sqlite)
* [Amazon DynamoDB](../tree/master/src/providers/WorkflowCore.Providers.AWS)
* [Redis](../tree/master/src/providers/WorkflowCore.Providers.Redis)
