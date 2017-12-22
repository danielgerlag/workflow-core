using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample17
{
    class CompensatingWorkflow : IWorkflow
    {
        public string Id => "compensate-sample";
        public int Version => 1;

        public void Build(IWorkflowBuilder<object> builder)
        {
            builder
                .StartWith(context => Console.WriteLine("Hello"))
                .Saga().Do(seq => seq
                    .StartWith(context => Console.WriteLine("1"))
                    .Then(context =>
                    {
                        Console.WriteLine("2");
                        throw new Exception("boo");
                        Console.WriteLine("2.5");
                    })                        
                    .Then(context => Console.WriteLine("3"))
                    )
                    .CompensateWith(context => Console.WriteLine("fail"))
                //.OnError(Models.WorkflowErrorHandling.)
                .Then(context => Console.WriteLine("end"));
        }
    }
}
