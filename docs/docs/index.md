# Workflow Core

Workflow Core is a light weight workflow engine targeting .NET Standard.  Think: long running processes with multiple tasks that need to track state.  It supports pluggable persistence and concurrency providers to allow for multi-node clusters.

## Installing

Install the NuGet package "WorkflowCore"

Using nuget
```
PM> Install-Package WorkflowCore
```

Using .net cli
```
dotnet add package WorkflowCore
```

## Fluent API

Define workflows with the fluent API.

```c#
public class MyWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder<MyData> builder)
    {    
        builder
           .StartWith<Task1>()
           .Then<Task2>()
           .Then<Task3>;
    }
}
```