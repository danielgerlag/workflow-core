# Error handling

Each step can be configured with it's own error handling behavior, it can be retried at a later time, suspend the workflow or terminate the workflow.

### Fluent API

```C#
public void Build(IWorkflowBuilder<object> builder)
{
    builder                
        .StartWith<HelloWorld>()
            .OnError(WorkflowErrorHandling.Retry, TimeSpan.FromMinutes(10))
        .Then<GoodbyeWorld>();
}
```

### JSON / YAML API

ErrorBehavior

```json
{
    "Id": "...",
    "StepType": "...",
    "ErrorBehavior": "Retry / Suspend / Terminate / Compensate",
    "RetryInterval": "00:10:00"
}
```
```yaml
Id: "..."
StepType: "..."
ErrorBehavior: Retry / Suspend / Terminate / Compensate
RetryInterval: '00:10:00'
```

## Global Error handling

The WorkflowHost service also has a `.OnStepError` event which can be used to intercept exceptions from workflow steps on a more global level.

## Custom Error Handlers 
All `IWorkflowErrorHandler` are registered when `.AddWorkflow()` is called. 
To create a custom error handler implement `IWorkflowErrorHandler` and add it as `Transient`.

Each `IWorkflowErrorHandler` should specify `WorkflowErrorHandling` which suggests what type of errors it should handle.
