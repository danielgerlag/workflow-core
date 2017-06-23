# User workflow extensions for Workflow Core

Provides extensions for [Workflow Core](../../README.md) to enable human workflows.

## Installing

Install the NuGet package "WorkflowCore.Users"

```
PM> Install-Package WorkflowCore.Users
```

## Usage

Use the .UserStep extension method when building your workflow.
Parameter 2 is a lambda function to resolve the user or group this step will be assigned to.

```C#
public class HumanWorkflow : IWorkflow
{
...
    public void Build(IWorkflowBuilder<object> builder)
    {
        builder
            .StartWith(context => .WriteLine("start"))
                .UserTask("Do you approve", data => "MYDOMAIN\\user")
                    .WithOption("yes", "I approve").Do(then => then
                        .StartWith(context => Console.WriteLine("You approved"))
                    )
                    .WithOption("no", "I do not approve").Do(then => then
                        .StartWith(context => Console.WriteLine("You did not approve"))
                    )
                .Then(context => Console.WriteLine("end"));
    }
  }
```

Get a list of available user actions for a given workflow with the .GetOpenUserActions on the WorkflowHost service.

```C#
var openItems = host.GetOpenUserActions(workflowId);
```

Respond to an open user action for a given workflow with the .PublishUserAction on the WorkflowHost service.

```C#
host.PublishUserAction(openItems.First().Key, "MYDOMAIN\\someuser", chosenValue);
```

[Sample App](../../samples/WorkflowCore.Sample08)

