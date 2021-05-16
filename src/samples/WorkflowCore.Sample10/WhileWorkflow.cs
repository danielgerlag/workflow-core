using System;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample10
{    
    public class WhileWorkflow : IWorkflow<MyData>
    {
        public string Id => "While";
        public int Version => 1;

        public void Build(IWorkflowBuilder<MyData> builder)
        {
            builder
                .StartWith<SayHello>()
                .While(data => data.Counter < 3)
                    .Do(x => x
                        .StartWith<DoSomething>()
                        .Then<IncrementStep>()
                            .Input(step => step.Value1, data => data.Counter)
                            .Output(data => data.Counter, step => step.Value2))
                .Then<SayGoodbye>();
        }        
    }

    public class MyData
    {
        public int Counter { get; set; }
    }
}
