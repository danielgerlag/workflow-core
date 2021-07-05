using System;
using WorkflowCore.Interface;
using WorkflowCore.Sample20.Steps;

namespace WorkflowCore.Sample20
{
    class ActivityWorkflow : IWorkflow<MyData>
    {
        public string Id => "activity-sample";
        public int Version => 1;

        public void Build(IWorkflowBuilder<MyData> builder)
        {
            builder
                .StartWith<HelloWorld>()
                .Activity((data, context) => "get-approval-" + context.Workflow.Id, (data) => data.Request)
                    .Output(data => data.ApprovedBy, step => step.Result)
                .Then<CustomMessage>()
                    .Input(step => step.Message, data => "Approved by " + data.ApprovedBy)
                .Then<GoodbyeWorld>();
        }
    }

    class MyData
    {
        public string Request { get; set; }
        public string ApprovedBy { get; set; }
    }
}
