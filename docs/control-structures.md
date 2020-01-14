# Control Structures

## Decision Branches

You can define multiple independent branches within your workflow and select one based on an expression value.

#### Fluent API

For the fluent API, we define our branches with the `CreateBranch()` method on the workflow builder.  We can then select a branch using the `Branch` method.

The select expressions will be matched to the branch listed via the `Branch` method, and the matching next step(s) will be scheduled to execute next.  Matching multiple next steps will result in parallel branches running.

This workflow will select `branch1` if the value of `data.Value1` is `one`, and `branch2` if it is `two`.
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
    .StartWith<HelloWorld>()
    .Decide(data => data.Value1)
        .Branch((data, outcome) => data.Value1 == "one", branch1)
        .Branch((data, outcome) => data.Value1 == "two", branch2);
```

#### JSON / YAML API

Hook up your branches via the `SelectNextStep` property, instead of a `NextStepId`.  The expressions will be matched to the step Ids listed in `SelectNextStep`, and the matching next step(s) will be scheduled to execute next.

```json
{
  "Id": "DecisionWorkflow",
  "Version": 1,
  "DataType": "MyApp.MyData, MyApp",
  "Steps": [
    {
      "Id": "decide",
      "StepType": "...",
      "SelectNextStep":
      {
        "Branch1": "<<result expression to match for branch 1>>",
        "Branch2": "<<result expression to match for branch 2>>"
      }
    },
    {
      "Id": "Branch1",
      "StepType": "MyApp.PrintMessage, MyApp",
      "Inputs": 
	  { 
		  "Message": "\"Hello from 1\"" 
	  }
    },
    {
      "Id": "Branch2",
      "StepType": "MyApp.PrintMessage, MyApp",
      "Inputs": 
	  { 
	    "Message": "\"Hello from 2\"" 
	  }
    }
  ]
}
```

```yaml
Id: DecisionWorkflow
Version: 1
DataType: MyApp.MyData, MyApp
Steps:
- Id: decide
  StepType: WorkflowCore.Primitives.Decide, WorkflowCore
  Inputs:
    Expression: <<input expression to evaluate>>
  OutcomeSteps:
    Branch1: '<<result expression to match for branch 1>>'
    Branch2: '<<result expression to match for branch 2>>'
- Id: Branch1
  StepType: MyApp.PrintMessage, MyApp
  Inputs:
    Message: '"Hello from 1"'
- Id: Branch2
  StepType: MyApp.PrintMessage, MyApp
  Inputs:
    Message: '"Hello from 2"'
```


## Parallel ForEach

Use the .ForEach method to start a parallel for loop

#### Fluent API

```C#
public class ForEachWorkflow : IWorkflow
{
    public string Id => "Foreach";
    public int Version => 1;

    public void Build(IWorkflowBuilder<object> builder)
    {
        builder
            .StartWith<SayHello>()
            .ForEach(data => new List<int>() { 1, 2, 3, 4 })
                .Do(x => x
                    .StartWith<DisplayContext>()
                        .Input(step => step.Message, (data, context) => context.Item)
                    .Then<DoSomething>())
            .Then<SayGoodbye>();
    }        
}
```

#### JSON / YAML API

```json
{
  "Id": "MyForEachStep",
  "StepType": "WorkflowCore.Primitives.ForEach, WorkflowCore",
  "NextStepId": "...",
  "Inputs": { "Collection": "<<expression to evaluate>>" },
  "Do": [[
      {
        "Id": "do1",
        "StepType": "MyApp.DoSomething1, MyApp",
        "NextStepId": "do2"
      },
      {
        "Id": "do2",
        "StepType": "MyApp.DoSomething2, MyApp"
      }
  ]]
}
```
```yaml
Id: MyForEachStep
StepType: WorkflowCore.Primitives.ForEach, WorkflowCore
NextStepId: "..."
Inputs:
  Collection: "<<expression to evaluate>>"
Do:
- - Id: do1
    StepType: MyApp.DoSomething1, MyApp
    NextStepId: do2
  - Id: do2
    StepType: MyApp.DoSomething2, MyApp
```


## While Loops

Use the .While method to start a while construct

#### Fluent API

```C#
public class WhileWorkflow : IWorkflow<MyData>
{
    public string Id => "While";
    public int Version => 1;

    public void Build(IWorkflowBuilder<MyData> builder)
    {
        builder
            .StartWith<SayHello>()
            .While(data => data.Counter < 3)
                .Do(x => x
                    .StartWith<DoSomething>()
                    .Then<IncrementStep>()
                        .Input(step => step.Value1, data => data.Counter)
                        .Output(data => data.Counter, step => step.Value2))
            .Then<SayGoodbye>();
    }        
}
```

#### JSON / YAML API

```json
{
  "Id": "MyWhileStep",
  "StepType": "WorkflowCore.Primitives.While, WorkflowCore",
  "NextStepId": "...",
  "Inputs": { "Condition": "<<expression to evaluate>>" },
  "Do": [[
      {
        "Id": "do1",
        "StepType": "MyApp.DoSomething1, MyApp",
        "NextStepId": "do2"
      },
      {
        "Id": "do2",
        "StepType": "MyApp.DoSomething2, MyApp"
      }
  ]]
}
```
```yaml
Id: MyWhileStep
StepType: WorkflowCore.Primitives.While, WorkflowCore
NextStepId: "..."
Inputs:
  Condition: "<<expression to evaluate>>"
Do:
- - Id: do1
    StepType: MyApp.DoSomething1, MyApp
    NextStepId: do2
  - Id: do2
    StepType: MyApp.DoSomething2, MyApp

```


## If Conditions

Use the .If method to start an if condition

#### Fluent API

```C#
public class IfWorkflow : IWorkflow<MyData>
{ 
    public void Build(IWorkflowBuilder<MyData> builder)
    {
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
    }        
}
```

#### JSON / YAML API

```json
{
    "Id": "MyIfStep",
    "StepType": "WorkflowCore.Primitives.If, WorkflowCore",
    "NextStepId": "...",
    "Inputs": { "Condition": "<<expression to evaluate>>" },
    "Do": [[
        {
          "Id": "do1",
          "StepType": "MyApp.DoSomething1, MyApp",
          "NextStepId": "do2"
        },
        {
          "Id": "do2",
          "StepType": "MyApp.DoSomething2, MyApp"
        }
    ]]
}
```
```yaml
Id: MyIfStep
StepType: WorkflowCore.Primitives.If, WorkflowCore
NextStepId: "..."
Inputs:
  Condition: "<<expression to evaluate>>"
Do:
- - Id: do1
    StepType: MyApp.DoSomething1, MyApp
    NextStepId: do2
  - Id: do2
    StepType: MyApp.DoSomething2, MyApp

```


## Parallel Paths

Use the .Parallel() method to branch parallel tasks

#### Fluent API

```C#
public class ParallelWorkflow : IWorkflow<MyData>
{
    public string Id => "parallel-sample";
    public int Version => 1;

    public void Build(IWorkflowBuilder<MyData> builder)
    {
        builder
            .StartWith<SayHello>()
            .Parallel()
                .Do(then => 
                    then.StartWith<Task1dot1>()
                        .Then<Task1dot2>()
                .Do(then =>
                    then.StartWith<Task2dot1>()
                        .Then<Task2dot2>()
                .Do(then =>
                    then.StartWith<Task3dot1>()
                        .Then<Task3dot2>()
            .Join()
            .Then<SayGoodbye>();
    }        
}
```

#### JSON / YAML API

```json
{
      "Id": "MyParallelStep",
      "StepType": "WorkflowCore.Primitives.Sequence, WorkflowCore",
      "NextStepId": "...",
      "Do": [
		[ 
		  {
		    "Id": "Branch1.Step1",
		    "StepType": "MyApp.DoSomething1, MyApp",
		    "NextStepId": "Branch1.Step2"
		  },
		  {
		    "Id": "Branch1.Step2",
		    "StepType": "MyApp.DoSomething2, MyApp"
		  }
		],			
		[ 
		  {
		    "Id": "Branch2.Step1",
		    "StepType": "MyApp.DoSomething1, MyApp",
		    "NextStepId": "Branch2.Step2"
		  },
		  {
		    "Id": "Branch2.Step2",
		    "StepType": "MyApp.DoSomething2, MyApp"
		  }
		]
	  ]
}
```
```yaml
Id: MyParallelStep
StepType: WorkflowCore.Primitives.Sequence, WorkflowCore
NextStepId: "..."
Do:
- - Id: Branch1.Step1
    StepType: MyApp.DoSomething1, MyApp
    NextStepId: Branch1.Step2
  - Id: Branch1.Step2
    StepType: MyApp.DoSomething2, MyApp
- - Id: Branch2.Step1
    StepType: MyApp.DoSomething1, MyApp
    NextStepId: Branch2.Step2
  - Id: Branch2.Step2
    StepType: MyApp.DoSomething2, MyApp
```


## Schedule

Use `.Schedule` to register a future set of steps to run asynchronously in the background within your workflow.

#### Fluent API

```c#
builder
    .StartWith(context => Console.WriteLine("Hello"))
    .Schedule(data => TimeSpan.FromSeconds(5)).Do(schedule => schedule
        .StartWith(context => Console.WriteLine("Doing scheduled tasks"))
    )
    .Then(context => Console.WriteLine("Doing normal tasks"));
```

#### JSON / YAML API

```json
{
  "Id": "MyScheduleStep",
  "StepType": "WorkflowCore.Primitives.Schedule, WorkflowCore",
  "Inputs": { "Interval": "<<expression to evaluate>>" },
  "Do": [[
      {
        "Id": "do1",
        "StepType": "MyApp.DoSomething1, MyApp",
        "NextStepId": "do2"
      },
      {
        "Id": "do2",
        "StepType": "MyApp.DoSomething2, MyApp"
      }
  ]]
}
```
```yaml
Id: MyScheduleStep
StepType: WorkflowCore.Primitives.Schedule, WorkflowCore
Inputs:
  Interval: "<<expression to evaluate>>"
Do:
- - Id: do1
    StepType: MyApp.DoSomething1, MyApp
    NextStepId: do2
  - Id: do2
    StepType: MyApp.DoSomething2, MyApp
```


### Delay

The `Delay` step will pause the current branch of your workflow for a specified period.

#### JSON / YAML API

```json
{
  "Id": "MyDelayStep",
  "StepType": "WorkflowCore.Primitives.Delay, WorkflowCore",
  "NextStepId": "...",
  "Inputs": { "Period": "<<expression to evaluate>>" }
}
```
```yaml
Id: MyDelayStep
StepType: WorkflowCore.Primitives.Delay, WorkflowCore
NextStepId: "..."
Inputs:
  Period: "<<expression to evaluate>>"
```


## Recur

Use `.Recur` to setup a set of recurring background steps within your workflow, until a certain condition is met

#### Fluent API

```c#
builder
    .StartWith(context => Console.WriteLine("Hello"))
    .Recur(data => TimeSpan.FromSeconds(5), data => data.Counter > 5).Do(recur => recur
        .StartWith(context => Console.WriteLine("Doing recurring task"))
    )
    .Then(context => Console.WriteLine("Carry on"));
```

#### JSON / YAML API

```json
{
  "Id": "MyScheduleStep",
  "StepType": "WorkflowCore.Primitives.Recur, WorkflowCore",
  "Inputs": { 
    "Interval": "<<expression to evaluate>>",
    "StopCondition": "<<expression to evaluate>>" 
  },
  "Do": [[
      {
        "Id": "do1",
        "StepType": "MyApp.DoSomething1, MyApp",
        "NextStepId": "do2"
      },
      {
        "Id": "do2",
        "StepType": "MyApp.DoSomething2, MyApp"
      }
  ]]
}
```
```yaml
Id: MyScheduleStep
StepType: WorkflowCore.Primitives.Recur, WorkflowCore
Inputs:
  Interval: "<<expression to evaluate>>"
  StopCondition: "<<expression to evaluate>>"
Do:
- - Id: do1
    StepType: MyApp.DoSomething1, MyApp
    NextStepId: do2
  - Id: do2
    StepType: MyApp.DoSomething2, MyApp
```

