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
## Workflow instances that fail to load

If a runnable instance cannot be loaded from persistence (for example, its stored data no longer deserializes after a data class was renamed), the consumer stops retrying it after 5 consecutive load failures. It logs an error saying the instance is quarantined, keeps it greylisted so the poller stops re-queuing it, and leaves the stored row unchanged so it can be repaired.

Quarantine is held in memory on each node and clears when the host restarts. A successful load resets the count. Failures while executing a step are not counted and keep using the normal retry behavior.

```c#
services.AddWorkflow(options => options.UseMaxWorkflowLoadAttempts(3));
```
