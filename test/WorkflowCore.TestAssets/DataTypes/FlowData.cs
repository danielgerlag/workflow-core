using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.TestAssets.Steps;

namespace WorkflowCore.TestAssets.DataTypes;

public class FlowData
{
    public AssigneeInfo Assignee { get; set; } = new();
    public List<AssigneeInfo> AssigneeList { get; set; } = [];
    public AssigneeInfo[] AssigneeArray { get; set; } = [];
}