# Workflow Core

[![Build status](https://ci.appveyor.com/api/projects/status/xnby6p5v4ur04u76?svg=true)](https://ci.appveyor.com/project/danielgerlag/workflow-core)

Workflow Core is a light weight embeddable workflow engine targeting .NET Standard.  Think: long running processes with multiple tasks that need to track state.  It supports pluggable persistence and concurrency providers to allow for multi-node clusters.

### Announcements

#### New related project: Conductor
Conductor is a stand-alone workflow server as opposed to a library that uses Workflow Core internally. It exposes an API that allows you to store workflow definitions, track running workflows, manage events and define custom steps and scripts for usage in your workflows.

https://github.com/danielgerlag/conductor

## Documentation

See [Tutorial here.](https://workflow-core.readthedocs.io)

## Fluent API

Define your workflows with the fluent API.

```c#
public class MyWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder<MyData> builder)
    {    
        builder
           .StartWith<Task1>()
           .Then<Task2>()
           .Then<Task3>();
    }
}
```

## JSON / YAML Workflow Definitions

Define your workflows in JSON or YAML, need to install WorkFlowCore.DSL

```json
{
  "Id": "HelloWorld",
  "Version": 1,
  "Steps": [
    {
      "Id": "Hello",
      "StepType": "MyApp.HelloWorld, MyApp",
      "NextStepId": "Bye"
    },        
    {
      "Id": "Bye",
      "StepType": "MyApp.GoodbyeWorld, MyApp"
    }
  ]
}
```

```yaml
Id: HelloWorld
Version: 1
Steps:
- Id: Hello
  StepType: MyApp.HelloWorld, MyApp
  NextStepId: Bye
- Id: Bye
  StepType: MyApp.GoodbyeWorld, MyApp
```

## Sample use cases

* New user workflow
```c#
public class MyData
{
	public string Email { get; set; }
	public string Password { get; set; }
	public string UserId { get; set; }
}

public class MyWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder<MyData> builder)
    {    
        builder
            .StartWith<CreateUser>()
                .Input(step => step.Email, data => data.Email)
                .Input(step => step.Password, data => data.Password)
                .Output(data => data.UserId, step => step.UserId)
           .Then<SendConfirmationEmail>()
               .WaitFor("confirmation", data => data.UserId)
           .Then<UpdateUser>()
               .Input(step => step.UserId, data => data.UserId);
    }
}
```

* Saga Transactions

```c#
public class MyWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder<MyData> builder)
    {    
        builder
            .StartWith<CreateCustomer>()
            .Then<PushToSalesforce>()
                .OnError(WorkflowErrorHandling.Retry, TimeSpan.FromMinutes(10))
            .Then<PushToERP>()
                .OnError(WorkflowErrorHandling.Retry, TimeSpan.FromMinutes(10));
    }
}
```

```c#
builder
    .StartWith<LogStart>()
    .Saga(saga => saga
        .StartWith<Task1>()
            .CompensateWith<UndoTask1>()
        .Then<Task2>()
            .CompensateWith<UndoTask2>()
        .Then<Task3>()
            .CompensateWith<UndoTask3>()
    )
    .OnError(Models.WorkflowErrorHandling.Retry, TimeSpan.FromMinutes(10))
    .Then<LogEnd>();
```

## Persistence

Since workflows are typically long running processes, they will need to be persisted to storage between steps.
There are several persistence providers available as separate Nuget packages.

* MemoryPersistenceProvider *(Default provider, for demo and testing purposes)*
* [MongoDB](src/providers/WorkflowCore.Persistence.MongoDB)
* [Cosmos DB](src/providers/WorkflowCore.Providers.Azure)
* [Amazon DynamoDB](src/providers/WorkflowCore.Providers.AWS)
* [SQL Server](src/providers/WorkflowCore.Persistence.SqlServer)
* [PostgreSQL](src/providers/WorkflowCore.Persistence.PostgreSQL)
* [Sqlite](src/providers/WorkflowCore.Persistence.Sqlite)
* [MySQL](src/providers/WorkflowCore.Persistence.MySQL)
* [Redis](src/providers/WorkflowCore.Providers.Redis)

## Search

A search index provider can be plugged in to Workflow Core, enabling you to index your workflows and search against the data and state of them.
These are also available as separate Nuget packages.
* [Elasticsearch](src/providers/WorkflowCore.Providers.Elasticsearch)

## Extensions

* [User (human) workflows](src/extensions/WorkflowCore.Users)


## Samples

* [Hello World](src/samples/WorkflowCore.Sample01)

* [Multiple outcomes](src/samples/WorkflowCore.Sample12)

* [Passing Data](src/samples/WorkflowCore.Sample03)

* [Parallel ForEach](src/samples/WorkflowCore.Sample09)

* [Sync ForEach](src/samples/WorkflowCore.Sample09s)

* [While Loop](src/samples/WorkflowCore.Sample10)

* [If Statement](src/samples/WorkflowCore.Sample11)

* [Events](src/samples/WorkflowCore.Sample04)

* [Activity Workers](src/samples/WorkflowCore.Sample18)

* [Parallel Tasks](src/samples/WorkflowCore.Sample13)

* [Saga Transactions (with compensation)](src/samples/WorkflowCore.Sample17)

* [Scheduled Background Tasks](src/samples/WorkflowCore.Sample16)

* [Recurring Background Tasks](src/samples/WorkflowCore.Sample14)

* [Dependency Injection](src/samples/WorkflowCore.Sample15)

* [Deferred execution & re-entrant steps](src/samples/WorkflowCore.Sample05)

* [Looping](src/samples/WorkflowCore.Sample02)

* [Exposing a REST API](src/samples/WebApiSample)

* [Human(User) Workflow](src/samples/WorkflowCore.Sample08)

* [Testing](src/samples/WorkflowCore.TestSample01)


## Contributors

* **Daniel Gerlag** - *Initial work*
* **Jackie Ja**
* **Aaron Scribner**
* **Roberto Paterlini**

## Related Projects

* [Conductor](https://github.com/danielgerlag/conductor) (Stand-alone workflow server built on Workflow Core)

## Ports

* [JWorkflow (Java)](https://github.com/danielgerlag/jworkflow)
* [workflow-es (Node.js)](https://github.com/danielgerlag/workflow-es)
* [liteflow (Python)](https://github.com/danielgerlag/liteflow)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

