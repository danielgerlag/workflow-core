using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.TestHost.CustomData;
using WorkflowCore.TestHost.CustomSteps;

namespace WorkflowCore.TestHost.Workflows
{
    public class PassingDataWorkflow : IWorkflow<MyDataClass>
    {
        public string Id
        {
            get
            {
                return "PassingDataWorkflow";
            }
        }

        public int Version
        {
            get
            {
                return 1;
            }
        }

        public void Build(IWorkflowBuilder<MyDataClass> builder)
        {
            builder
                .StartWith(context =>
                {
                    Console.WriteLine("start 123");
                    return new ExecutionResult(null);
                })
                .Then<AddNumbers>()
                    .Input(step => step.Input1, data => data.Value1)
                    .Input(step => step.Input2, data => data.Value2)
                    .Output(data => data.Value3, step => step.Output)
                .Then<CustomMessage>()
                    .Name("Print custom message")
                    .Input(step => step.Message, data => "The answer is " + data.Value3.ToString())
                .Then(context =>
                    {
                        Console.WriteLine("from inline step");
                        return new ExecutionResult(null);
                    });
        }
    }
}
