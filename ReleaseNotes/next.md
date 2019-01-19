
## Action Inputs / Outputs

Added the action Input & Output overloads on the fluent step builder.

```c#
Input(Action<TStepBody, TData> action);
```

This will allow one to manipulate properties on the step before it executes and properties on the data object after it executes, for example

```c#
Input((step, data) => step.Value1 = data.Value1)
```

```c#
.Output((step, data) => data["Value3"] = step.Output)
```

```c#
.Output((step, data) => data.MyCollection.Add(step.Output))
```

## Breaking changes

The existing ability to assign values to entries in dictionaries or dynamic objects on `.Output` was problematic, 
since it broke the ability to pass collections on the Output mappings.


```c#
.Output(data => data["Value3"], step => step.Output)
```

This feature has been removed, and it is advised to use the action Output API instead, for example


```c#
.Output((step, data) => data["Value3"] = step.Output)
```

This functionality remains intact for JSON defined workflows.