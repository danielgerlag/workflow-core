using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.TestHost.CustomSteps;

namespace WorkflowCore.TestHost.Workflows
{
    public class SimpleDecisionWorkflow : IWorkflow
    {
        public string Id
        {
            get
            {
                return "Simple Decision Workflow";
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
                .StartWith<HelloWorld>()
                .Then<RandomOutput>(randomOutput =>
                {
                    randomOutput.When(0)
                        .Then<CustomMessage>(cm =>
                        {
                            cm.Name("Print custom message");
                            cm.Input(step => step.Message, data => "BOO!!!");
                        })
                        .Then(randomOutput);  //loop back to randomOutput

                    randomOutput.When(1)
                        .Then<HelloWorld>();
                });
        }
    }
}
