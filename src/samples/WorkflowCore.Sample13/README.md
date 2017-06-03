# Parallel sample

Illustrates how to run several branches of steps in parallel, and then join once all are complete.


```c#
builder
    .StartWith<SayHello>()
    .Parallel()
        .Do(then => 
            then.StartWith<PrintMessage>()
                    .Input(step => step.Message, data => "Item 1.1")
                .Then<PrintMessage>()
                    .Input(step => step.Message, data => "Item 1.2"))
        .Do(then =>
            then.StartWith<PrintMessage>()
                    .Input(step => step.Message, data => "Item 2.1")
                .Then<PrintMessage>()
                    .Input(step => step.Message, data => "Item 2.2"))
        .Do(then =>
            then.StartWith<PrintMessage>()
                    .Input(step => step.Message, data => "Item 3.1")
                .Then<PrintMessage>()
                    .Input(step => step.Message, data => "Item 3.2"))
    .Join()
    .Then<SayGoodbye>();
```
