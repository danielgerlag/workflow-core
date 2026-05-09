# Forking Workflows

Forking creates a new independent workflow instance from a running one, continuing from the exact same active step(s).  This is useful when you need to split workloads mid-execution without changing the source instance.

## Use Cases

- Splitting batch processing so a subset of items can continue in a new instance
- Creating parallel independent processing paths at runtime
- Dynamic workload distribution based on runtime conditions

## API Reference

```csharp
// Fork a running workflow instance
string forkedId = await host.ForkWorkflow(workflowId);

// Fork with data mutation
string forkedId = await host.ForkWorkflow(workflowId, data => {
    ((MyDataClass)data).Quantity = 3;
});
```

## Behavior Details

- The forked instance gets a new ID and continues from the same active/pending step(s) as the source
- Only active/pending execution pointers and their scope chain are cloned (no full execution history)
- Workflow data is deep-cloned via JSON serialization, so the two instances are fully independent
- Event subscriptions are recreated for any steps waiting for events
- The source workflow instance is unmodified and continues normally
- Both instances can be managed independently (suspend, resume, terminate)

## Calling from External Code

`ForkWorkflow` should be called from **outside** a running step — for example from a controller, background job, or the application's `Main` method. The workflow executor holds a distributed lock on the instance while a step executes, so calling `ForkWorkflow` on the same instance from inside a step will deadlock.

A safe pattern is to have the workflow pause at a `WaitFor` step, fork from external code, then publish an event to resume both instances:

```csharp
// Workflow definition — pauses at a WaitFor step
builder
    .StartWith<ProcessItems>()
    .WaitFor("ForkDecision", data => data.EventKey, data => DateTime.Now)
    .Then<ProcessItems>()
    .Then<Complete>();

// External code — fork while the workflow is paused
var forkedId = await host.ForkWorkflow(workflowId, data =>
{
    var batch = (MyDataClass)data;
    var overflow = batch.Items.Skip(10).ToList();
    batch.Items = overflow;
});

// Resume both by publishing the event
await host.PublishEvent("ForkDecision", eventKey, null);
```

## Limitations

- Workflow data must be JSON-serializable (`Newtonsoft.Json` is used internally)
- The source workflow must be in `Runnable` or `Suspended` status
- Forking acquires a distributed lock on the source instance — **do not call `ForkWorkflow` from within a step of the same workflow instance**, as this will deadlock
- If the workflow is actively executing (not paused at a `WaitFor` or `Sleep`), the lock may not be immediately available
