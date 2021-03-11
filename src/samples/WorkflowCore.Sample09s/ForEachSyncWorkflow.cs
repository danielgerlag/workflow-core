using System;
using System.Collections.Generic;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample09s
{    
    public class ForEachSyncWorkflow : IWorkflow
    {
        public string Id => "ForeachSync";
        public int Version => 1;

        public void Build(IWorkflowBuilder<object> builder)
        {
            builder
                .StartWith<SayHello>()
                .ForEach(data => new List<int> { 1, 2, 3, 4 }, data => false)
                    .Do(x => x
                        .StartWith<DisplayContext>()
                            .Input(step => step.Item, (data, context) => context.Item)
                        .Then<DoSomething>())
                .Then<SayGoodbye>();
        }        
    }    
}
