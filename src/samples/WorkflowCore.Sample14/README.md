# Recur sample

Illustrates how to run a set of recurring background steps within your workflow, until a certain condition is met


```c#
builder
    .StartWith(context => Console.WriteLine("Hello"))
    .Recur(data => TimeSpan.FromSeconds(5), data => data.Counter > 5).Do(recur => recur
        .StartWith(context => Console.WriteLine("Doing recurring task"))
    )
    .Then(context => Console.WriteLine("Carry on"));
```
