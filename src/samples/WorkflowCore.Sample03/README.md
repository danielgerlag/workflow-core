# Passing data between steps sample

Illustrates how to define a data class for your workflow and wire it's properties up to the inputs and outputs of steps.

First, we define a class to hold data for our workflow.
```C#
public class MyDataClass
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public int Value3 { get; set; }
}
```

Then we create a step with inputs and outputs, by simply exposing public properties.
```C#
public class AddNumbers : StepBody
{
    public int Input1 { get; set; }
    public int Input2 { get; set; }
    public int Output { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Output = (Input1 + Input2);
        return ExecutionResult.Next();
    }
}
```

Then we put it all together in a workflow.

```C#
public class PassingDataWorkflow : IWorkflow<MyDataClass>
{  
    public void Build(IWorkflowBuilder<MyDataClass> builder)
    {
        builder
            .StartWith(context =>
            {
                Console.WriteLine("Starting workflow...");
                return ExecutionResult.Next();
            })
            .Then<AddNumbers>()
                .Input(step => step.Input1, data => data.Value1)
                .Input(step => step.Input2, data => data.Value2)
                .Output(data => data.Value3, step => step.Output)
            .Then<CustomMessage>()
                .Name("Print custom message")
                .Input(step => step.Message, data => "The answer is " + data.Value3.ToString())
            .Then(context =>
                {
                    Console.WriteLine("Workflow complete");
                    return ExecutionResult.Next();
                });
    }
	...
}
```

