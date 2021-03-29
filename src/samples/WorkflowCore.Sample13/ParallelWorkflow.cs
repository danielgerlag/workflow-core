using System;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample13
{    
    public class ParallelWorkflow : IWorkflow<MyData>
    {
        public string Id => "parallel-sample";
        public int Version => 1;

        public void Build(IWorkflowBuilder<MyData> builder)
        {
            builder
                .StartWith<SayHello>()
                .Parallel()
                    .Do(then => 
                        then.StartWith<PrintMessage>()
                                .Input(step => step.Message, data => "Item 1.1")
                            .Then<PrintMessage>()
                                .Input(step => step.Message, data => "Item 1.2"))
                    .Do(then =>
                        then.StartWith<PrintMessage>()
                                .Input(step => step.Message, data => "Item 2.1")
                            .Then<PrintMessage>()
                                .Input(step => step.Message, data => "Item 2.2")
                            .Then<PrintMessage>()
                                .Input(step => step.Message, data => "Item 2.3"))
                    .Do(then =>
                        then.StartWith<PrintMessage>()
                                .Input(step => step.Message, data => "Item 3.1")
                            .Then<PrintMessage>()
                                .Input(step => step.Message, data => "Item 3.2"))
                .Join()
                .Then<SayGoodbye>();
        }        
    }

    public class MyData
    {
        public int Counter { get; set; }
    }
}
