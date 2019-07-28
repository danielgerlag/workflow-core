# Basic Concepts

## Steps

A workflow consists of a series of connected steps.  Each step can have inputs and produce outputs that can be passed back to the workflow within which it exists.

Steps are defined by creating a class that inherits from the `StepBody` or `StepBodyAsync` abstract classes and implementing the Run/RunAsync method.  They can also be created inline while defining the workflow structure.

### First we define some steps

```C#
public class HelloWorld : StepBody
{
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Console.WriteLine("Hello world");
        return ExecutionResult.Next();
    }
}
```
*The `StepBody` and `StepBodyAsync` class implementations are constructed by the workflow host which first tries to use IServiceProvider for dependency injection, if it can't construct it with this method, it will search for a parameterless constructor*

### Then we define the workflow structure by composing a chain of steps.  The is done by implementing the IWorkflow interface

```C#
public class HelloWorldWorkflow : IWorkflow
{
    public string Id => "HelloWorld";
    public int Version => 1;

    public void Build(IWorkflowBuilder<object> builder)
    {
        builder
            .StartWith<HelloWorld>()
            .Then<GoodbyeWorld>();
    }  
}
```
The *IWorkflow* interface also has a readonly Id property and readonly Version property.  These are used by the workflow host to identify a workflow definition.

This workflow implemented in JSON would look like this
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


### You can also define your steps inline

```C#
public class HelloWorldWorkflow : IWorkflow
{
    public string Id => "HelloWorld";
    public int Version => 1;

    public void Build(IWorkflowBuilder<object> builder)
    {
        builder
            .StartWith(context =>
            {
                Console.WriteLine("Hello world");
                return ExecutionResult.Next();
            })
            .Then(context =>
            {
                Console.WriteLine("Goodbye world");
                return ExecutionResult.Next();
            });
    }
}
```

Each running workflow is persisted to the chosen persistence provider between each step, where it can be picked up at a later point in time to continue execution.  The outcome result of your step can instruct the workflow host to defer further execution of the workflow until a future point in time or in response to an external event.

## Host

The workflow host is the service responsible for executing workflows.  It does this by polling the persistence provider for workflow instances that are ready to run, executes them and then passes them back to the persistence provider to by stored for the next time they are run.  It is also responsible for publishing events to any workflows that may be waiting on one.

### Setup

Use the *AddWorkflow* extension method for *IServiceCollection* to configure the workflow host upon startup of your application.
By default, it is configured with *MemoryPersistenceProvider* and *SingleNodeConcurrencyProvider* for testing purposes.  You can also configure a DB persistence provider at this point.

```C#
services.AddWorkflow();
```

### Usage

When your application starts, grab the workflow host from the built-in dependency injection framework *IServiceProvider*.  Make sure you call *RegisterWorkflow*, so that the workflow host knows about all your workflows, and then call *Start()* to fire up the thread pool that executes workflows.  Use the *StartWorkflow* method to initiate a new instance of a particular workflow.


```C#
var host = serviceProvider.GetService<IWorkflowHost>();            
host.RegisterWorkflow<HelloWorldWorkflow>();
host.Start();

host.StartWorkflow("HelloWorld", 1, null);

Console.ReadLine();
host.Stop();
```

## Passing data between steps

Each step is intended to be a black-box, therefore they support inputs and outputs.  These inputs and outputs can be mapped to a data class that defines the custom data relevant to each workflow instance.

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
        return ExecutionResult.Next();
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

or in jSON format
```json
{
  "Id": "AddWorkflow",
  "Version": 1,
  "DataType": "MyApp.MyDataClass, MyApp",
  "Steps": [
	{
      "Id": "Add",
      "StepType": "MyApp.AddNumbers, MyApp",
      "NextStepId": "ShowResult",
      "Inputs": { 
          "Value1": "data.Value1",
          "Value2": "data.Value2" 
       },
      "Outputs": { 
          "Answer": "step.Output" 
      }
    },    
    {
      "Id": "ShowResult",
      "StepType": "MyApp.CustomMessage, MyApp",
      "Inputs": { 
          "Message": "\"The answer is \" + data.Value1" 
       }
    }
  ]
}
```

or in YAML format
```yaml
Id: AddWorkflow
Version: 1
DataType: MyApp.MyDataClass, MyApp
Steps:
- Id: Add
  StepType: MyApp.AddNumbers, MyApp
  NextStepId: ShowResult
  Inputs:
    Value1: data.Value1
    Value2: data.Value2
  Outputs:
    Answer: step.Output
- Id: ShowResult
  StepType: MyApp.CustomMessage, MyApp
  Inputs:
    Message: '"The answer is " + data.Value1'
```


## Injecting dependencies into steps

If you register your step classes with the IoC container, the workflow host will use the IoC container to construct them and therefore inject any required dependencies.  This example illustrates the use of dependency injection for workflow steps.

Consider the following service

```C#
public interface IMyService
{
    void DoTheThings();
}
...
public class MyService : IMyService
{
    public void DoTheThings()
    {
        Console.WriteLine("Doing stuff...");
    }
}
```

Which is consumed by a workflow step as follows

```C#
public class DoSomething : StepBody
{
    private IMyService _myService;

    public DoSomething(IMyService myService)
    {
        _myService = myService;
    }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        _myService.DoTheThings();
        return ExecutionResult.Next();
    }
}
```

Simply add both the service and the workflow step as transients to the service collection when setting up your IoC container.
(Avoid registering steps as singletons, since multiple concurrent workflows may need to use them at once.)

```C#
IServiceCollection services = new ServiceCollection();
services.AddLogging();
services.AddWorkflow();
            
services.AddTransient<DoSomething>();
services.AddTransient<IMyService, MyService>();
```


