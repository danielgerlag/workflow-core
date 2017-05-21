# If sample

Illustrates how to implement an If decision within your workflow.


```c#
builder
    .StartWith<SayHello>()
    .If(data => data.Counter < 3).Do(then => then
        .StartWith<PrintMessage>()
            .Input(step => step.Message, data => "Value is less than 3")
    )
    .If(data => data.Counter < 5).Do(then => then
        .StartWith<PrintMessage>()
            .Input(step => step.Message, data => "Value is less than 5")
    )
    .Then<SayGoodbye>();
```
