# Multiple outcomes sample

Illustrates how to fork a workflow with multiple outcomes of a particular step.

This workflow will start with a step that will generate a random number of 0 or 1, and emit this value as an outcome.
Then, based on the result, the workflow can take two paths, either (TaskA + TaskB) or (TaskC + TaskD)

```C#
public class MultipleOutcomeWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder<object> builder)
    {
        builder
            .StartWith<RandomOutput>(x => x.Name("Random Step"))
                .When(0)
                    .Then<TaskA>()
                    .Then<TaskB>()                        
                    .End<RandomOutput>("Random Step")
                .When(1)
                    .Then<TaskC>()
                    .Then<TaskD>()
                    .End<RandomOutput>("Random Step");
    }
}
```
