# Loading workflow definitions from JSON or YAML

Install the `WorkflowCore.DSL` package from nuget and call `AddWorkflowDSL` on your service collection.
Then grab the `DefinitionLoader` from the IoC container and call the `.LoadDefinition` method

```c#
using WorkflowCore.Interface;
...
var loader = serviceProvider.GetService<IDefinitionLoader>();
loader.LoadDefinition("<<json or yaml string here>>", Deserializers.Json);
```

## Common DSL

Both the JSON and YAML formats follow a common DSL, where step types within the workflow are referenced by the fully qualified class names.
Built-in step types typically live in the `WorklfowCore.Primitives` namespace.

| Field                   | Description                 |
| ----------------------- | --------------------------- |
| Id                      | Workflow Definition ID        |
| Version                 | Workflow Definition Version   |
| DataType                | Fully qualified assembly class name of the custom data object   |
| Steps[].Id              | Step ID (required unique key for each step)                     |
| Steps[].StepType        | Fully qualified assembly class name of the step                 |
| Steps[].NextStepId      | Step ID of the next step after this one completes               |
| Steps[].Inputs          | Optional Key/value pair of step inputs                          |
| Steps[].Outputs         | Optional Key/value pair of step outputs                         |
| Steps[].CancelCondition | Optional cancel condition                                       |

```json
{
  "Id": "HelloWorld",
  "Version": 1,
  "Steps": [
    {
      "Id": "Hello",
      "StepType": "MyApp.HelloWorld, MyApp",
      "NextStepId": "Bye"
    },        
    {
      "Id": "Bye",
      "StepType": "MyApp.GoodbyeWorld, MyApp"
    }
  ]
}
```
```yaml
Id: HelloWorld
Version: 1
Steps:
- Id: Hello
  StepType: MyApp.HelloWorld, MyApp
  NextStepId: Bye
- Id: Bye
  StepType: MyApp.GoodbyeWorld, MyApp
```

### Inputs and Outputs

Inputs and outputs can be bound to a step as a key/value pair object, 
* The `Inputs` collection, the key would match a property on the `Step` class and the value would be an expression with both the `data` and `context` parameters at your disposal.
* The `Outputs` collection, the key would match a property on the `Data` class and the value would be an expression with both the `step` as a parameter at your disposal.

Full details of the capabilities of  expression language can be found [here](https://github.com/StefH/System.Linq.Dynamic.Core/wiki/Dynamic-Expressions#expression-language)

```json
{
  "Id": "AddWorkflow",
  "Version": 1,
  "DataType": "MyApp.MyDataClass, MyApp",
  "Steps": [
    {
      "Id": "Hello",
      "StepType": "MyApp.HelloWorld, MyApp",
      "NextStepId": "Add"
    },
	{
      "Id": "Add",
      "StepType": "MyApp.AddNumbers, MyApp",
      "NextStepId": "Bye",
      "Inputs": { 
          "Value1": "data.Value1",
          "Value2": "data.Value2" 
       },
      "Outputs": { 
          "Answer": "step.Result" 
      }
    },    
    {
      "Id": "Bye",
      "StepType": "MyApp.GoodbyeWorld, MyApp"
    }
  ]
}
```
```yaml
Id: AddWorkflow
Version: 1
DataType: MyApp.MyDataClass, MyApp
Steps:
- Id: Hello
  StepType: MyApp.HelloWorld, MyApp
  NextStepId: Add
- Id: Add
  StepType: MyApp.AddNumbers, MyApp
  NextStepId: Bye
  Inputs:
    Value1: data.Value1
    Value2: data.Value2
  Outputs:
    Answer: step.Result
- Id: Bye
  StepType: MyApp.GoodbyeWorld, MyApp
```

```json
{
  "Id": "AddWorkflow",
  "Version": 1,
  "DataType": "MyApp.MyDataClass, MyApp",
  "Steps": [
    {
      "Id": "Hello",
      "StepType": "MyApp.HelloWorld, MyApp",
      "NextStepId": "Print"
    },
    {
      "Id": "Print",
      "StepType": "MyApp.PrintMessage, MyApp",
      "Inputs": { "Message": "\"Hi there!\"" }
    }
  ]
}
```
```yaml
Id: AddWorkflow
Version: 1
DataType: MyApp.MyDataClass, MyApp
Steps:
- Id: Hello
  StepType: MyApp.HelloWorld, MyApp
  NextStepId: Print
- Id: Print
  StepType: MyApp.PrintMessage, MyApp
  Inputs:
    Message: '"Hi there!"'

```

### WaitFor

The `.WaitFor` can be implemented using inputs as follows

| Field                  | Description                 |
| ---------------------- | --------------------------- |
| CancelCondition        | Optional expression to specify a cancel condition  |
| Inputs.EventName       | Expression to specify the event name               |
| Inputs.EventKey        | Expression to specify the event key                |
| Inputs.EffectiveDate   | Optional expression to specify the effective date  |


```json
{
    "Id": "MyWaitStep",
    "StepType": "WorkflowCore.Primitives.WaitFor, WorkflowCore",
    "NextStepId": "...",
    "CancelCondition": "...",
    "Inputs": {
        "EventName": "\"Event1\"",
        "EventKey": "\"Key1\"",
        "EffectiveDate": "DateTime.Now"
    }
}
```
```yaml
Id: MyWaitStep
StepType: WorkflowCore.Primitives.WaitFor, WorkflowCore
NextStepId: "..."
CancelCondition: "..."
Inputs:
  EventName: '"Event1"'
  EventKey: '"Key1"'
  EffectiveDate: DateTime.Now

```

### Activity

The `.Activity` can be implemented using inputs as follows

| Field                  | Description                 |
| ---------------------- | --------------------------- |
| CancelCondition        | Optional expression to specify a cancel condition  |
| Inputs.ActivityName    | Expression to specify the activity name            |
| Inputs.Parameters      | Expression to specify the parameters to pass the activity worker                |
| Inputs.EffectiveDate   | Optional expression to specify the effective date  |


```json
{
    "Id": "MyActivityStep",
    "StepType": "WorkflowCore.Primitives.Activity, WorkflowCore",
    "NextStepId": "...",
    "CancelCondition": "...",
    "Inputs": {
        "ActivityName": "\"my-activity\"",
        "Parameters": "data.SomeValue"
    }
}
```
```yaml
Id: MyActivityStep
StepType: WorkflowCore.Primitives.Activity, WorkflowCore
NextStepId: "..."
CancelCondition: "..."
Inputs:
  ActivityName: '"my-activity"'
  EventKey: '"Key1"'
  Parameters: data.SomeValue

```


### If

The `.If` can be implemented as follows

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

### While

The `.While` can be implemented as follows

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

### ForEach

The `.ForEach` can be implemented as follows

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

### Delay

The `.Delay` can be implemented as follows

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


### Parallel

The `.Parallel` can be implemented as follows

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

### Schedule

The `.Schedule` can be implemented as follows

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

### Recur

The `.Recur` can be implemented as follows

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
