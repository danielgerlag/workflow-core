using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Sample06.Steps;

namespace WorkflowCore.Sample06
{    
    public class MultipleOutcomeWorkflow : IWorkflow
    {
        public string Id => "MultipleOutcomeWorkflow";

        public int Version => 1;

        public void Build(IWorkflowBuilder<object> builder)
        {
            builder
                .StartWith<RandomOutput>(x => x.Name("Random Step"))
                    .When(0)
                        .Then<TaskA>()
                        .Then<TaskB>()                        
                        .End<RandomOutput>("Random Step")
                    .When(1)
                        .Then<TaskC>()
                        .Then<TaskD>()
                        .End<RandomOutput>("Random Step");
        }
    }
}
