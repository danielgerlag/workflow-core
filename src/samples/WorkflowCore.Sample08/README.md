# Human (User) Workflow Sample

This sample demonstrates how to create workflows that require human interaction using the WorkflowCore.Users extension.

## What this sample shows

* **User Tasks**: How to create tasks that are assigned to specific users or groups
* **User Options**: How to provide multiple choice options for users to select from
* **Conditional Branching**: How to execute different workflow paths based on user choices
* **Task Escalation**: How to automatically reassign tasks to different users when timeouts occur
* **User Action Management**: How to retrieve open user actions and publish user responses programmatically

## The Workflow

```c#
public class HumanWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder<object> builder)
    {
        builder
            .StartWith(context => ExecutionResult.Next())
            .UserTask("Do you approve", data => @"domain\bob")
                .WithOption("yes", "I approve").Do(then => then
                    .StartWith(context => Console.WriteLine("You approved"))
                )
                .WithOption("no", "I do not approve").Do(then => then
                    .StartWith(context => Console.WriteLine("You did not approve"))
                )
                .WithEscalation(x => TimeSpan.FromSeconds(20), x => @"domain\frank", action => action
                    .StartWith(context => Console.WriteLine("Escalated task"))
                    .Then(context => Console.WriteLine("Sending notification..."))
                    )
            .Then(context => Console.WriteLine("end"));
    }
}
```

## How it works

1. **Task Assignment**: The workflow creates a user task with the prompt "Do you approve" and assigns it to `domain\bob`

2. **User Options**: Two options are provided:
   - "yes" with label "I approve" - executes approval workflow
   - "no" with label "I do not approve" - executes rejection workflow

3. **Escalation**: If the task is not completed within 20 seconds, it automatically escalates to `domain\frank` and executes the escalation workflow

4. **User Interaction**: The program demonstrates how to:
   - Get open user actions using `host.GetOpenUserActions(workflowId)`
   - Display options to the user
   - Publish user responses using `host.PublishUserAction(key, user, value)`

## Key Features

* **UserTask**: Creates tasks that wait for human input
* **WithOption**: Defines multiple choice options with conditional workflow paths
* **WithEscalation**: Automatically reassigns tasks after a timeout period
* **Interactive Console**: Shows how to build a simple interface for user interaction

## Dependencies

This sample requires the `WorkflowCore.Users` extension package, which provides the human workflow capabilities.

## Use Cases

This pattern is useful for:
- Approval workflows
- Decision-making processes
- Task assignment and escalation
- Interactive business processes
- Multi-step user interactions