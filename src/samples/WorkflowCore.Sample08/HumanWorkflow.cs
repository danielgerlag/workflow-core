using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample08
{
    public class HumanWorkflow : IWorkflow
    {
        public string Id
        {
            get
            {
                return "HumanWorkflow";
            }
        }

        public int Version
        {
            get
            {
                return 1;
            }
        }

        public void Build(IWorkflowBuilder<object> builder)
        {
            var step1 = builder.StartWith(context => ExecutionResult.Next());
            var step2 = step1.UserStep("Do you agree", data => "MYDOMAIN\\daniel");
            step2
                .When("yes", "I agree")
                .Then(context =>
                {
                    Console.WriteLine("You agreed");
                    return ExecutionResult.Next();
                });

            step2
                .When("no", "I do not agree")
                .Then(context =>
                {
                    Console.WriteLine("You did not agree");
                    return ExecutionResult.Next();
                });

        }
    }
}

