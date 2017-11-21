# Schedule sample

Illustrates how to schedule a future set of steps to run asynchronously in the background within your workflow.


```c#
builder
    .StartWith(context => Console.WriteLine("Hello"))
    .Schedule(data => TimeSpan.FromSeconds(5)).Do(schedule => schedule
        .StartWith(context => Console.WriteLine("Doing scheduled tasks"))
    )
    .Then(context => Console.WriteLine("Doing normal tasks"));
```
