using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
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
