# Decision and branch sample

Illustrates how to switch different workflow paths based on an expression value.


You can define multiple independent branches within your workflow and select one based on an expression value.

For the fluent API, we define our branches with the `CreateBranch()` method on the workflow builder.  We can then select a branch using the `Decide` step.

Use the `Decide` primitive step and hook up your branches via the `Branch` method.  The result of the input expression will be matched to the expressions listed via the `Branch` method, and the matching next step(s) will be scheduled to execute next.


```c#
var branch1 = builder.CreateBranch()
    .StartWith<PrintMessage>()
        .Input(step => step.Message, data => "hi from 1")
    .Then<PrintMessage>()
        .Input(step => step.Message, data => "bye from 1");

var branch2 = builder.CreateBranch()
    .StartWith<PrintMessage>()
        .Input(step => step.Message, data => "hi from 2")
    .Then<PrintMessage>()
        .Input(step => step.Message, data => "bye from 2");


builder
    .StartWith<SayHello>()
    .Decide(data => data.Value)
        .Branch(1, branch1)
        .Branch(2, branch2);
```
