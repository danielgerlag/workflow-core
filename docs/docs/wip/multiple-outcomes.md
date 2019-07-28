### Multiple outcomes / forking

A workflow can take a different path depending on the outcomes of preceeding steps.  The following example shows a process where first a random number of 0 or 1 is generated and is the outcome of the first step.  Then, depending on the outcome value, the workflow will either fork to (TaskA + TaskB) or (TaskC + TaskD)

```C#
public class MultipleOutcomeWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder<object> builder)
    {
        builder
            .StartWith<RandomOutput>(x => x.Name("Random Step"))
                .When(data => 0).Do(then => then
                    .StartWith<TaskA>()
                    .Then<TaskB>())
                .When(data => 1).Do(then => then
                    .StartWith<TaskC>()
                    .Then<TaskD>())
            .Then<SayGoodbye>();
    }
}
```