# Events sample

Illustrates how to signal a workflow to wait for an external event, and how to invoke that event.

We use the .WaitFor() method on the workflow builder to signal the current execution path to wait until an event with a particular name (MyEvent) and key (0) is published to the workflow host.
```C#
public class EventSampleWorkflow : IWorkflow<MyDataClass>
{
    public void Build(IWorkflowBuilder<MyDataClass> builder)
    {
        builder
            .StartWith(context => ExecutionResult.Next())
            .WaitFor("MyEvent", data => "0")
                .Output(data => data.StrValue, step => step.EventData)
            .Then<CustomMessage>() 
                .Input(step => step.Message, data => "The data from the event is " + data.StrValue)
            .Then(context =>
            {
                Console.WriteLine("workflow complete");
                return ExecutionResult.Next();
            });
    }
}
```
An event is published to all subscribed workflows via the IWorkflowHost service, where a data object can be passed to all workflows waiting for event (MyEvent) with key 0.

```C#
Console.WriteLine("Enter value to publish");
string value = Console.ReadLine();
host.PublishEvent("MyEvent", "0", value);
```

