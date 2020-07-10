# Foreach Sync sample

Illustrates how to implement a synchronous foreach within your workflow.


```c#
builder
	.StartWith<SayHello>()
	.ForEach(data => new List<int>() { 1, 2, 3, 4 }, data => false)
		.Do(x => x
			.StartWith<DisplayContext>()
				.Input(step => step.Item, (data, context) => context.Item)
			.Then<DoSomething>())
	.Then<SayGoodbye>();
```

or get the collection from workflow data.

```c#
builder
	.StartWith<SayHello>()
	.ForEach(data => data.MyCollection, data => false)
		.Do(x => x
			.StartWith<DisplayContext>()
				.Input(step => step.Item, (data, context) => context.Item)
			.Then<DoSomething>())
	.Then<SayGoodbye>();

```
