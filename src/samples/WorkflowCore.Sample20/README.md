# Activity sample

Illustrates how to have your workflow wait for an external activity that is fulfilled by a worker that you implement.

This workflow will wait for the `get-approval-{workflowId}`  activity and pass the request string to it as an input.

The main reason behind of this example is Activities are global listeners. Therefore, in some cases, you want to be sure about its uniqueness.

Also, you can take look the issue https://github.com/danielgerlag/workflow-core/issues/542

```c#
builder
    .StartWith<HelloWorld>()
    .Activity((data, context) => context.Workflow.Id, (data) => data.Request)
        .Output(data => data.ApprovedBy, step => step.Result)
    .Then<CustomMessage>()
        .Input(step => step.Message, data => "Approved by " + data.ApprovedBy)
    .Then<GoodbyeWorld>();
```

Then we implement an activity worker to pull pending activities of type `get-approval`, where we can inspect the input and submit a response back to the waiting workflow.

```c#
var workflowId = host.StartWorkflow("activity-sample", new MyData { Request = "Spend $1,000,000" }).Result;

var approval = host.GetPendingActivity("get-approval-" + workflowId, "worker1", TimeSpan.FromMinutes(1)).Result;

if (approval != null)
{                
    Console.WriteLine("Approval required for " + approval.Parameters);
    host.SubmitActivitySuccess(approval.Token, "John Smith");
}
```

