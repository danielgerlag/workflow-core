using System;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample12
{    
    public class OutcomeWorkflow : IWorkflow<MyData>
    {
        public string Id => "outcome-sample";
        public int Version => 1;

        public void Build(IWorkflowBuilder<MyData> builder)
        {
            var branch1 = builder.CreateBranch()
                .StartWith<PrintMessage>()
                    .Input(step => step.Message, data => "hi from 1")
                .Then<PrintMessage>()
                    .Input(step => step.Message, data => "bye from 1");

            var branch2 = builder.CreateBranch()
                .StartWith<PrintMessage>()
                    .Input(step => step.Message, data => "hi from 2")
                .Then<PrintMessage>()
                    .Input(step => step.Message, data => "bye from 2");


            builder
                .StartWith<SayHello>()
                .Decide(data => data.Value)
                    .Branch(1, branch1)
                    .Branch(2, branch2);
        }        
    }

    public class MyData
    {
        public int Value { get; set; }
    }
}
