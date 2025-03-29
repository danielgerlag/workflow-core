using System;
using System.Linq;
using WorkflowCore.TestAssets.Steps;

namespace WorkflowCore.TestAssets.DataTypes;

public class FlowData
{
    public AssigneeInfo Assignee { get; set; } = new();
}