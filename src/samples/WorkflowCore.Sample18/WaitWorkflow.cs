using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Sample18.Steps;

namespace WorkflowCore.Sample18
{
    class WaitWorkflow : IWorkflow
    {
        public string Id => nameof(WaitWorkflow);

        public int Version => 1;

        public void Build(IWorkflowBuilder<object> builder)
        {
            builder.StartWith<Step1>();
        }
    }
}