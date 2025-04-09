using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.TestAssets.DataTypes;

namespace WorkflowCore.TestAssets.Steps;

public class AssignTask : StepBody
{       
    public AssigneeInfo? Assignee { get; set; }
    public List<AssigneeInfo> AssigneeList { get; set; }
    
    public AssigneeInfo[] AssigneeArray { get; set; } = [];

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (context.Workflow.Data is FlowData flowData)
        {
            if (Assignee != null)
            {
                flowData.Assignee = new AssigneeInfo
                {
                    Id = Assignee.Id,
                    Name = Assignee.Name,
                    MemberType = Assignee.MemberType,
                    UnitInfo = Assignee.UnitInfo
                };
            }

            flowData.AssigneeList.AddRange(AssigneeList);
            flowData.AssigneeArray = AssigneeArray.ToArray();
        }
        return ExecutionResult.Next();
    }
}