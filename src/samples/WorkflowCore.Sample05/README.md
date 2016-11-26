# Deferred execution sample

Illustrates how to put a workflow execution path to sleep.

This feature is invoked by the execution result of a step.  So here we created a step that will return a *SleepResult* on the first pass, then a normal *OutcomeResult* on the second pass.
```C#
public class SleepStep : StepBody
{        
    public TimeSpan Period { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (context.PersistenceData == null)
            return ExecutionResult.Sleep(Period, new object());
        else
            return ExecutionResult.Next();
    }
}
```

```C#
public class DeferSampleWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder<object> builder)
    {
        builder
            .StartWith(context =>
            {
                Console.WriteLine("Workflow started");                    
                return ExecutionResult.Next();
            })
            .Then<SleepStep>()
                .Input(step => step.Period, data => TimeSpan.FromSeconds(20))
            .Then(context =>
            {
                Console.WriteLine("workflow complete");
                return ExecutionResult.Next();
            });
    }
	...
}
```

