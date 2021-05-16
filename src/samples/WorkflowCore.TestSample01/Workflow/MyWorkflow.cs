using System;
using WorkflowCore.Interface;

namespace WorkflowCore.TestSample01.Workflow
{
    public class MyWorkflow : IWorkflow<MyDataClass>
    {
        public string Id => "MyWorkflow";
        public int Version => 1;
        public void Build(IWorkflowBuilder<MyDataClass> builder)
        {
            builder
                .StartWith<AddNumbers>()
                    .Input(step => step.Input1, data => data.Value1)
                    .Input(step => step.Input2, data => data.Value2)
                    .Output(data => data.Value3, step => step.Output);
        }
    }
}
