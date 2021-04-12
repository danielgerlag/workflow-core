using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Sample15.Steps;

namespace WorkflowCore.Sample15
{
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder<object> builder)
        {
            builder                
                .StartWith<HelloWorld>()
                .Then<DoSomething>();
        }

        public string Id => "HelloWorld";
            
        public int Version => 1;
                 
    }
}
