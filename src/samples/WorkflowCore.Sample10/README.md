# While sample

Illustrates how to implement a while loop within your workflow.


```c#
builder
    .StartWith<SayHello>()
    .While(data => data.Counter < 3)
        .Do(x => x
            .StartWith<DoSomething>()
            .Then<IncrementStep>()
                .Input(step => step.Value1, data => data.Counter)
                .Output(data => data.Counter, step => step.Value2))
    .Then<SayGoodbye>();
```
