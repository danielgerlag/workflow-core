using System;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample11
{    
    public class IfWorkflow : IWorkflow<MyData>
    {
        public string Id => "if-sample";
        public int Version => 1;

        public void Build(IWorkflowBuilder<MyData> builder)
        {
            builder
                .StartWith<SayHello>()
                .If(data => data.Counter < 3).Do(then => then
                    .StartWith<PrintMessage>()
                        .Input(step => step.Message, data => "Value is less than 3")
                )
                .If(data => data.Counter < 5).Do(then => then
                    .StartWith<PrintMessage>()
                        .Input(step => step.Message, data => "Value is less than 5")
                )
                .Then<SayGoodbye>();
        }        
    }

    public class MyData
    {
        public int Counter { get; set; }
    }
}
