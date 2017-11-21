# Dependency Injection Sample

Illustrates the use of dependency injection for workflow steps.

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

```C#
IServiceCollection services = new ServiceCollection();
services.AddLogging();
services.AddWorkflow();
            
services.AddTransient<DoSomething>();
services.AddTransient<IMyService, MyService>();
```
