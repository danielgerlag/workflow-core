# Error handling

Each step can be configured with it's own error handling behavior, it can be retried at a later time, suspend the workflow or terminate the workflow.

```C#
public void Build(IWorkflowBuilder<object> builder)
{
    builder                
        .StartWith<HelloWorld>()
            .OnError(WorkflowErrorHandling.Retry, TimeSpan.FromMinutes(10))
        .Then<GoodbyeWorld>();
}
```

The WorkflowHost service also has a .OnStepError event which can be used to intercept exceptions from workflow steps on a more global level.