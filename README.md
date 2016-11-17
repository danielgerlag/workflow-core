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
The *IWorkflow* interface also has a readonly Id property and readonly Version property.  These are generally static and are used by the workflow runtime to identify a workflow definition.

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

Each running workflow is persisted to the chosen persistence provider between each step, where it can be picked up at a later point in time to continue execution.  The outcome result of your step can instruct the workflow runtime to defer further execution of the workflow until a future point in time or in response to an external event.

The first time a particular step within the workflow is called, the PersistenceData property on the context object is *null*.  The ExecutionResult produced by the Run method can either cause the workflow to proceed to the next step by providing an outcome value, instruct the workflow to sleep for a defined period or simply not move the workflow forward.  If no outcome value is produced, then the step becomes re-entrant by setting PersistenceData, so the workflow runtime will call this step again in the future buy will popluate the PersistenceData with it's previous value.

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
            .StartWith(context =>
            {
                Console.WriteLine("Starting workflow...");
                return new ExecutionResult(null);
            })
            .Then<AddNumbers>()
                .Input(step => step.Input1, data => data.Value1)
                .Input(step => step.Input2, data => data.Value2)
                .Output(data => data.Value3, step => step.Output)
            .Then<CustomMessage>()
                .Input(step => step.Message, data => "The answer is " + data.Value3.ToString())
            .Then(context =>
                {
                    Console.WriteLine("Workflow comeplete");
                    return new ExecutionResult(null);
                });
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
                return new ExecutionResult(null);
            })
            .WaitFor("MyEvent", "0")
                .Output(data => data.StrValue, step => step.EventData)
            .Then<CustomMessage>()
                .Input(step => step.Message, data => "The data from the event is " + data.StrValue)
            .Then(context =>
            {
                Console.WriteLine("workflow complete");
                return new ExecutionResult(null);
            });
    }
}
...
//External events are published via the runtime
//All workflows that have subscribed to MyEvent 0, will be passed "hello"
runtime.PublishEvent("MyEvent", "0", "hello");
```

### Persistence

*todo*

### Concurrency

*todo*

*work in progress...*


## Authors

* **Daniel Gerlag** - *Initial work*

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details


