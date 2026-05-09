# Forking Workflows sample

Illustrates how to fork a running workflow instance so the new workflow continues from the same active step with different data.

The workflow pauses at a `WaitFor` step, giving external code the opportunity to fork and mutate data before resuming both instances.

```csharp
builder
    .StartWith<ProcessBatch>()
    .WaitFor("ForkDecision", data => data.EventKey, data => DateTime.Now)
    .Then<ProcessBatch>()
    .Then(context => Console.WriteLine($"Workflow {context.Workflow.Id} complete"));
```

The fork is driven from `Program.cs` — **not** from within a step body — to avoid deadlocks (the workflow executor holds a distributed lock on the instance while a step executes).

```csharp
// Wait for workflow to reach the WaitFor step
// Then fork, mutating the data to split items
var forkedId = host.ForkWorkflow(workflowId, data =>
{
    var batch = (BatchData)data;
    batch.Items = batch.Items.Skip(batch.Threshold).ToList();
}).Result;

// Publish event to resume both
host.PublishEvent("ForkDecision", eventKey, null);
```

Expected output:

```text
Started workflow 0b0f... with 12 items
Workflow 0b0f... processing 12 item(s): [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]
Forked workflow 75c6... with overflow items
Workflow 0b0f... processing 5 item(s): [1, 2, 3, 4, 5]
Workflow 75c6... processing 7 item(s): [6, 7, 8, 9, 10, 11, 12]
Workflow 0b0f... complete with 5 item(s).
Workflow 75c6... complete with 7 item(s).
Both workflows completed. Press enter to stop.
```
