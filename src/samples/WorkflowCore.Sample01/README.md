# Hello World Sample

Illustrates the basic usage of the fluent API for defining workflows.

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

and how to define steps

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


