# Workflow Core

Workflow Core is a light weight workflow engine targeting .NET Standard 1.6.  It supports pluggable persistence and concurrency providers to allow for multi-node clusters.

## Installing

Install the NuGet package "WorkflowCore"

```
Install-Package WorkflowCore -Pre
```


## Basic Concepts

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

or define your steps inline

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

*work in progress...*


## Authors

* **Daniel Gerlag** - *Initial work*

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details


