# User workflow extensions for Workflow Core

Provides extensions for [Workflow Core](../../README.md) to enable human workflows.

## Installing

Install the NuGet package "WorkflowCore.Users"

```
PM> Install-Package WorkflowCore.Users -Pre
```

## Usage

Use the .UserStep extension method when building your workflow.

```C#
public class HumanWorkflow : IWorkflow
{
...
    public void Build(IWorkflowBuilder<object> builder)
    {
        var step1 = builder.StartWith(context => ExecutionResult.Next());
        var step2 = step1.UserStep("Do you agree", data => "MYDOMAIN\\daniel");
        step2
            .When("yes", "I agree")
            .Then(context =>
            {
                Console.WriteLine("You agreed");
                return ExecutionResult.Next();
            });

        step2
            .When("no", "I do not agree")
            .Then(context =>
            {
                Console.WriteLine("You did not agree");
                return ExecutionResult.Next();
            });

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


