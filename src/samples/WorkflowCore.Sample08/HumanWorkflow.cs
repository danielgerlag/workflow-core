using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Users.Models;

namespace WorkflowCore.Sample08
{
    public class HumanWorkflow : IWorkflow
    {
        public string Id => "HumanWorkflow";
            
        public int Version => 1;
            
        public void Build(IWorkflowBuilder<object> builder)
        {
            builder
                .StartWith(context => ExecutionResult.Next())
                .UserTask("Do you approve", data => "MYDOMAIN\\user")
                    .WithOption("yes", "I approve").Do(then => then
                        .StartWith(context => Console.WriteLine("You approved"))
                    )
                    .WithOption("no", "I do not approve").Do(then => then
                        .StartWith(context => Console.WriteLine("You did not approve"))
                    )
                .Then(context => Console.WriteLine("end"));
        }
    }
}

