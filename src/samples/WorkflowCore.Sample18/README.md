# Activity sample

Illustrates how to have your workflow wait for an external activity that is fulfilled by a worker that you implement.

This workflow will wait for the `get-approval` activity and pass the request string to it as an input.

```c#
builder
    .StartWith<HelloWorld>()
    .Activity("get-approval", (data) => data.Request)
        .Output(data => data.ApprovedBy, step => step.Result)
    .Then<CustomMessage>()
        .Input(step => step.Message, data => "Approved by " + data.ApprovedBy)
    .Then<GoodbyeWorld>();
```

Then we implement an activity worker to pull pending activities of type `get-approval`, where we can inspect the input and submit a response back to the waiting workflow.

```c#
var approval = host.GetPendingActivity("get-approval", "worker1", TimeSpan.FromMinutes(1)).Result;

if (approval != null)
{                
    Console.WriteLine("Approval required for " + approval.Parameters);
    host.SubmitActivitySuccess(approval.Token, "John Smith");
}
```

