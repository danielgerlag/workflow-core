# Foreach sample

Illustrates how to implement a parallel foreach within your workflow.


```c#
builder
	.StartWith<SayHello>()
	.ForEach(data => new List<int>() { 1, 2, 3, 4 })
		.Do(x => x
			.StartWith<DisplayContext>()
			.Then<DoSomething>())
	.Then<SayGoodbye>();
```

or get the collectioin from workflow data.

```c#
builder
	.StartWith<SayHello>()
	.ForEach(data => data.MyCollection)
		.Do(x => x
			.StartWith<DisplayContext>()
			.Then<DoSomething>())
	.Then<SayGoodbye>();

```

Access the iteration item from the step execution context

```c#
public class DisplayContext : StepBody
{        
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Console.WriteLine($"Working on item {context.Item}");
        return ExecutionResult.Next();
    }
}
```
