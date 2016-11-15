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
    public class EventSampleWorkflow : IWorkflow<MyDataClass>
    {
        public string Id
        {
            get
            {
                return "EventSampleWorkflow";
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
                .WaitFor("MyEvent", "0")
                    .Output(data => data.StrValue, step => step.EventData)
                .Then<CustomMessage>()
                    .Name("Print custom message")
                    .Input(step => step.Message, data => "The answer is " + data.StrValue)
                .Then(context =>
                {
                    Console.WriteLine("from inline step");
                    return new ExecutionResult(null);
                });
        }
    }
}
