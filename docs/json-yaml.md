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

You can also pass object graphs to step inputs as opposed to just scalar values
```json
"inputs": 
{    
  "Body": {
      "Value1": 1,
      "Value2": 2
  },
  "Headers": {
      "Content-Type": "application/json"
  }
},
```

If you want to evaluate an expression for a given property of your object, simply prepend and `@` and pass an expression string
```json
"inputs": 
{    
  "Body": {
      "@Value1": "data.MyValue * 2",
      "Value2": 5
  },
  "Headers": {
      "Content-Type": "application/json"
  }
},
```

#### Enums

If your step has an enum property, you can just pass the string representation of the enum value and it will be automatically converted.

#### Environment variables available in input expressions

You can access environment variables from within input expressions.
usage:
```
environment["VARIABLE_NAME"]
```

