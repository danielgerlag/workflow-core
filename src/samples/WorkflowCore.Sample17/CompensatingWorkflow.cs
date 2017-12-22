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
                    .CompensateWith(context => Console.WriteLine("fail hello"))
                .Saga(saga => saga
                    .StartWith(context => Console.WriteLine("1"))
                        .CompensateWith(context => Console.WriteLine("fail 1"))
                    .Then(context =>
                    {
                        Console.WriteLine("2");
                        throw new Exception("boo");
                        Console.WriteLine("2.5");
                    })
                        .CompensateWith(context => Console.WriteLine("fail 2"))
                    .Then(context => Console.WriteLine("3"))
                    )
                    .CompensateWith(context => Console.WriteLine("fail saga"))
                //.OnError(Models.WorkflowErrorHandling.)
                .Then(context => Console.WriteLine("end"));
        }
    }
}
