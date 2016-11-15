# Workflow Core

Workflow Core is a light weight workflow engine targeting .NET Standard 1.6.  It supports pluggable persistence and concurrency providers to allow for multi-node clusters.

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

    public string Id
    {
        get { return "HelloWorld"; }
    }

    public int Version 
    { 
        get { return 1; }
    }        
}
```

Each running workflow is persisted to the chosen persistence provider between each step, where it can be picked up at a later point in time to continue execution.  The outcome result of your step can instruct the workflow runtime to defer further execution of the workflow until a future point in time or in response to an external event.

*work in progress...*


## Authors

* **Daniel Gerlag** - *Initial work*

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details


