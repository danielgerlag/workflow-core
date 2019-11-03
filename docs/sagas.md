# Saga transaction with compensation

A Saga allows you to encapsulate a sequence of steps within a saga transaction and specify compensation steps for each.

In the sample, `Task2` will throw an exception, then `UndoTask2` and `UndoTask1` will be triggered.

```c#
builder
    .StartWith(context => Console.WriteLine("Begin"))
    .Saga(saga => saga
        .StartWith<Task1>()
            .CompensateWith<UndoTask1>()
        .Then<Task2>()
            .CompensateWith<UndoTask2>()
        .Then<Task3>()
            .CompensateWith<UndoTask3>()
    )
        .CompensateWith<CleanUp>()
    .Then(context => Console.WriteLine("End"));
```

## Retry policy for failed saga transaction

This particular example will retry the saga every 5 seconds, but you could also simply fail completely, and process a master compensation task for the whole saga.

```c#
builder
    .StartWith(context => Console.WriteLine("Begin"))
    .Saga(saga => saga
        .StartWith<Task1>()
            .CompensateWith<UndoTask1>()
        .Then<Task2>()
            .CompensateWith<UndoTask2>()
        .Then<Task3>()
            .CompensateWith<UndoTask3>()
    )
    .OnError(Models.WorkflowErrorHandling.Retry, TimeSpan.FromSeconds(5))
    .Then(context => Console.WriteLine("End"));
```

## Compensate entire saga transaction

You could also only specify a master compensation step, as follows

```c#
builder
    .StartWith(context => Console.WriteLine("Begin"))
        .Saga(saga => saga
            .StartWith<Task1>()
            .Then<Task2>()
            .Then<Task3>()
    )
        .CompensateWith<UndoEverything>()
    .Then(context => Console.WriteLine("End"));
```

## Passing parameters to compensation steps

Parameters can be passed to a compensation step as follows

```c#
builder
    .StartWith<SayHello>()
        .CompensateWith<PrintMessage>(compensate => 
        {
            compensate.Input(step => step.Message, data => "undoing...");
        })
```

## Expressing a saga in JSON or YAML

A saga transaction can be expressed in JSON or YAML, by using the `WorkflowCore.Primitives.Sequence` step and setting the `Saga` parameter to `true`.

The compensation steps can be defined by specifying the `CompensateWith` parameter.

```json
{
  "Id": "Saga-Sample",
  "Version": 1,
  "DataType": "MyApp.MyDataClass, MyApp",
  "Steps": [
    {
      "Id": "Hello",
      "StepType": "MyApp.HelloWorld, MyApp",
      "NextStepId": "MySaga"
    },    
    {
      "Id": "MySaga",
      "StepType": "WorkflowCore.Primitives.Sequence, WorkflowCore",
      "NextStepId": "Bye",
      "Saga": true,
      "Do": [
        [
          {
            "Id": "do1",
            "StepType": "MyApp.Task1, MyApp",
            "NextStepId": "do2",
            "CompensateWith": [
              {
                "Id": "undo1",
                "StepType": "MyApp.UndoTask1, MyApp"
              }
            ]
          },
          {
            "Id": "do2",
            "StepType": "MyApp.Task2, MyApp",
            "CompensateWith": [
              {
                "Id": "undo2-1",
                "NextStepId": "undo2-2",
                "StepType": "MyApp.UndoTask2, MyApp"
              },
              {
                "Id": "undo2-2",
                "StepType": "MyApp.DoSomethingElse, MyApp"
              }
            ]
          }
        ]
      ]
    },    
    {
      "Id": "Bye",
      "StepType": "MyApp.GoodbyeWorld, MyApp"
    }
  ]
}
```

```yaml
Id: Saga-Sample
Version: 1
DataType: MyApp.MyDataClass, MyApp
Steps:
- Id: Hello
  StepType: MyApp.HelloWorld, MyApp
  NextStepId: MySaga
- Id: MySaga
  StepType: WorkflowCore.Primitives.Sequence, WorkflowCore
  NextStepId: Bye
  Saga: true
  Do:
  - - Id: do1
      StepType: MyApp.Task1, MyApp
      NextStepId: do2
      CompensateWith:
      - Id: undo1
        StepType: MyApp.UndoTask1, MyApp
    - Id: do2
      StepType: MyApp.Task2, MyApp
      CompensateWith:
      - Id: undo2-1
        NextStepId: undo2-2
        StepType: MyApp.UndoTask2, MyApp
      - Id: undo2-2
        StepType: MyApp.DoSomethingElse, MyApp
- Id: Bye
  StepType: MyApp.GoodbyeWorld, MyApp

```
