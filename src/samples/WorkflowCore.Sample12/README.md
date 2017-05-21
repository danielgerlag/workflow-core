# Outcome sample

Illustrates how to switch different workflow paths based on a step outcome

First we need a workflow step with a specific outcome.

```c#
public class DetermineSomething : StepBody
{
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        return ExecutionResult.Outcome(2);
    }
}
```

Then we use the .When().Do method to determine which sub-path we take.

```c#
builder
    .StartWith<SayHello>()
    .Then<DetermineSomething>()
        .When(data => 1).Do(then => then
            .StartWith<PrintMessage>()
                .Input(step => step.Message, data => "Outcome was 1")
        )
        .When(data => 2).Do(then => then
            .StartWith<PrintMessage>()
                .Input(step => step.Message, data => "Outcome was 2")
        )                
    .Then<SayGoodbye>();
```
