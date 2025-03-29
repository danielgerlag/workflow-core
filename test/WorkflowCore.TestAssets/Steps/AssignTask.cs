using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.TestAssets.DataTypes;

namespace WorkflowCore.TestAssets.Steps;

public class AssignTask : StepBody
{       
    public AssigneeInfo Assignee { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (context.Workflow.Data is FlowData flowData)
        {
            flowData.Assignee = new AssigneeInfo
            {
                Id = Assignee.Id,
                Name = Assignee.Name,
                MemberType = Assignee.MemberType,
                UnitInfo = Assignee.UnitInfo
            };
        }
        return ExecutionResult.Next();
    }
}