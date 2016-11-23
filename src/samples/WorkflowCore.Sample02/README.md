# Looping through workflow steps sample

Illustrates how to create a workflow that loops.

```C#
public class SimpleDecisionWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder<object> builder)
    {
        builder
            .StartWith<HelloWorld>()
            .Then<RandomOutput>(randomOutput =>
            {
                randomOutput
                    .When(0)
                        .Then<CustomMessage>(cm =>
                        {
                            cm.Name("Print custom message");
                            cm.Input(step => step.Message, data => "Looping back....");
                        })
                        .Then(randomOutput);  //loop back to randomOutput

                randomOutput
                    .When(1)
                        .Then<GoodbyeWorld>();
            });
    }
	...
}
```

