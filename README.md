# Workflow Core

Workflow Core is a light weight workflow engine targeting .NET Standard 1.6.  It supports pluggable persistence and concurrency providers to allow for multi-node clusters.

## Installing

Install the NuGet package "WorkflowCore"

```
Install-Package WorkflowCore -Pre
```


## Basic Concepts

### Steps

A workflow consists of a series of connected steps.  Each step produces an outcome value and subsequent steps are triggered by subscribing to a particular outcome of a preceeding step.  The default outcome of *null* can be used for a basic linear workflow.
Steps are usually defined by inheriting from the StepBody abstract class and implementing the Run method.  They can also be created inline while defining the workflow structure.

First we define some steps

```C#
public class HelloWorld : StepBody
{
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Console.WriteLine("Hello world");
        return OutcomeResult(null);
    }
}
```
*The StepBody class implementations are constructed by the workflow host which first tries to use IServiceProvider from the built-in dependency injection of .NET Core, if it can't construct it with this method, it will search for a parameterless constructor*

Then we define the workflow structure by composing a chain of steps.  The is done by implementing the IWorkflow interface.

```C#
public class HelloWorldWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder<object> builder)
    {
        builder
            .StartWith<HelloWorld>()
            .Then<GoodbyeWorld>();
    }  
    ...
}
```
The *IWorkflow* interface also has a readonly Id property and readonly Version property.  These are generally static and are used by the workflow host to identify a workflow definition.

You can also define your steps inline

```C#
public class HelloWorldWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder<object> builder)
    {
        builder
            .StartWith(context =>
            {
                Console.WriteLine("Hello world");
                return new ExecutionResult(null);
            })
            .Then(context =>
            {
                Console.WriteLine("Goodbye world");
                return new ExecutionResult(null);
            })
    }
    ...
}
```

Each running workflow is persisted to the chosen persistence provider between each step, where it can be picked up at a later point in time to continue execution.  The outcome result of your step can instruct the workflow host to defer further execution of the workflow until a future point in time or in response to an external event.

The first time a particular step within the workflow is called, the PersistenceData property on the context object is *null*.  The ExecutionResult produced by the Run method can either cause the workflow to proceed to the next step by providing an outcome value, instruct the workflow to sleep for a defined period or simply not move the workflow forward.  If no outcome value is produced, then the step becomes re-entrant by setting PersistenceData, so the workflow host will call this step again in the future buy will popluate the PersistenceData with it's previous value.

For example, this step will initially run with *null* PersistenceData and put the workflow to sleep for 12 hours, while setting the PersistenceData to *new Object()*.  12 hours later, the step will be called again but context.PersistenceData will now contain the object constructed in the previous iteration, and will now produce an outcome value of *null*, causing the workflow to move forward.

```C#
public class SleepStep : StepBody
{
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (context.PersistenceData == null)
            return SleepResult(new Object(), Timespan.FromHours(12));
        else
            return OutcomeResult(null);
    }
}
```

### Passing data between steps

Each step is intented to be a blackbox, therefore they support inputs and outputs.  These inputs and outputs can be mapped to a data class that defines the custom data relevant to each workflow instance.

The following sample shows how to define inputs and outputs on a step, it then shows how define a workflow with a typed class for internal data and how to map the inputs and outputs to properties on the custom data class.

```C#
//Our workflow step with inputs and outputs
public class AddNumbers : StepBody
{
    public int Input1 { get; set; }

    public int Input2 { get; set; }

    public int Output { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Output = (Input1 + Input2);
        return OutcomeResult(null);
    }
}

//Our class to define the internal data of our workflow
public class MyDataClass
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public int Value3 { get; set; }
}

//Our workflow definition with strongly typed internal data and mapped inputs & outputs
public class PassingDataWorkflow : IWorkflow<MyDataClass>
{  
    public void Build(IWorkflowBuilder<MyDataClass> builder)
    {
        builder            
            .StartWith<AddNumbers>()
                .Input(step => step.Input1, data => data.Value1)
                .Input(step => step.Input2, data => data.Value2)
                .Output(data => data.Value3, step => step.Output)
            .Then<CustomMessage>()
                .Input(step => step.Message, data => "The answer is " + data.Value3.ToString());
    }
    ...
}

```

### Events

A workflow can also wait for an external event before proceeding.  In the following example, the workflow will wait for an event called *"MyEvent"* with a key of *0*.  Once an external source has fired this event, the workflow will wake up and continute processing, passing the data generated by the event onto the next step.

```C#
public class EventSampleWorkflow : IWorkflow<MyDataClass>
{
    public void Build(IWorkflowBuilder<MyDataClass> builder)
    {
        builder
            .StartWith(context =>
            {
                Console.WriteLine("workflow started");
                return new ExecutionResult(null);
            })
            .WaitFor("MyEvent", "0")
                .Output(data => data.Value, step => step.EventData)
            .Then<CustomMessage>()
                .Input(step => step.Message, data => "The data from the event is " + data.Value);
    }
}
...
//External events are published via the host
//All workflows that have subscribed to MyEvent 0, will be passed "hello"
host.PublishEvent("MyEvent", "0", "hello");
```

### Host

The workflow host is the service responsible for executing workflows.  It does this by polling the persistence provider for workflow instances that are ready to run, executes them and then passes them back to the persistence provider to by stored for the next time they are run.  It is also responsible for publishing events to any workflows that may be waiting on one.

#### Setup

Use the *AddWorkflow* extension method for *IServiceCollection* to configure the workflow host upon startup of your application.
By default, it is configured with *MemoryPersistenceProvider* and *SingleNodeConcurrencyProvider* for testing purposes.  You can also configure a DB persistence provider at this point.

```C#
services.AddWorkflow();
```

#### Usage

When your application starts, grab the workflow host from the built-in dependency injection framework *IServiceProvider*.  Make sure you call *RegisterWorkflow*, so that the workflow host knows about all your workflows, and then call *Start()* to fire up the thread pool that executes workflows.  Use the *StartWorkflow* method to initiate a new instance of a particular workflow.


```C#
var host = serviceProvider.GetService<IWorkflowHost>();            
host.RegisterWorkflow<HelloWorldWorkflow>();
host.Start();

host.StartWorkflow("HelloWorld", 1, null);

Console.ReadLine();
host.Stop();
```


### Persistence

Since workflows are typically long running processes, they will need to be persisted to storage between steps.
There are several persistence providers available as seperate Nuget packages.

* MemoryPersistenceProvider *(Default provider, for demo and testing purposes)*
* [MongoDB](src/providers/WorkflowCore.Persistence.MongoDB)
* [SQL Server](src/providers/WorkflowCore.Persistence.SqlServer)
* [PostgreSQL](src/providers/WorkflowCore.Persistence.PostgreSQL)
* [Sqlite](src/providers/WorkflowCore.Persistence.Sqlite)

### Concurrency

*todo*

*work in progress...*

## Samples

[Hello World](src/samples/WorkflowCore.Sample01)

[Multiple outcomes](src/samples/WorkflowCore.Sample06)

[Passing Data](src/samples/WorkflowCore.Sample03)

[Events](src/samples/WorkflowCore.Sample04)

[Deferred execution & re-entrant steps](src/samples/WorkflowCore.Sample05)

[Looping](src/samples/WorkflowCore.Sample02)


## Status

![status](https://ci.appveyor.com/api/projects/status/github/danielgerlag/workflow-core)

## Authors

* **Daniel Gerlag** - *Initial work*

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details


