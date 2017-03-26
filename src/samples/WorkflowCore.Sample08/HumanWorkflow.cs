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
                .UserStep("Do you approve", data => "MYDOMAIN\\user", x => x.Name("Approval Step"))            
                    .When("yes", "I approve")
                        .Then(context =>
                        {
                            Console.WriteLine("You approved");
                            return ExecutionResult.Next();
                        })
                    .End<UserStep>("Approval Step")            
                    .When("no", "I do not approve")
                        .Then(context =>
                        {
                            Console.WriteLine("You did not approve");
                            return ExecutionResult.Next();
                        })
                    .End<UserStep>("Approval Step");

        }
    }
}

