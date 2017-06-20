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
        public string Id => "EventSampleWorkflow";
            
        public int Version => 1;
            
        public void Build(IWorkflowBuilder<MyDataClass> builder)
        {
            builder
                .StartWith(context => ExecutionResult.Next())
                    .Name("start")
                .Recur(x => TimeSpan.FromSeconds(10), x => !string.IsNullOrEmpty(x.StrValue))
                //.Recur(x => TimeSpan.FromSeconds(10), x => false)
                    .Do(x => x.StartWith(context => Console.WriteLine(DateTime.Now.ToString())).Name("write"))

                .WaitFor("MyEvent", data => "0", data => DateTime.Now)
                    .Output(data => data.StrValue, step => step.EventData)
                    .Name("wait")
                .Then<CustomMessage>()
                    .Name("Print custom message")
                    .Input(step => step.Message, data => "The data from the event is " + data.StrValue)
                .Then(context => Console.WriteLine("workflow complete"));
        }
    }
}
