using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Sample04.Steps;

namespace WorkflowCore.Sample04
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
                .StartWith(context => new ExecutionResult(null))
                .WaitFor("MyEvent", "0")
                    .Output(data => data.StrValue, step => step.EventData)
                .Then<CustomMessage>()
                    .Name("Print custom message")
                    .Input(step => step.Message, data => "The data from the event is " + data.StrValue)
                .Then(context =>
                {
                    Console.WriteLine("workflow complete");
                    return new ExecutionResult(null);
                });
        }
    }
}
