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
            builder
                .StartWith(context => ExecutionResult.Next())
                .UserStep("Choose", data => "MYDOMAIN\\daniel")
                .Then(context =>
                {
                    Console.WriteLine("workflow complete");
                    return ExecutionResult.Next();
                });
        }
    }
}

