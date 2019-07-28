# Persistence

Since workflows are typically long running processes, they will need to be persisted to storage between steps.
There are several persistence providers available as separate Nuget packages.

* MemoryPersistenceProvider *(Default provider, for demo and testing purposes)*
* [MongoDB](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Persistence.MongoDB)
* [SQL Server](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Persistence.SqlServer)
* [PostgreSQL](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Persistence.PostgreSQL)
* [Sqlite](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Persistence.Sqlite)
* [Amazon DynamoDB](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.AWS)
* [Redis](https://github.com/danielgerlag/workflow-core/tree/master/src/providers/WorkflowCore.Providers.Redis)
