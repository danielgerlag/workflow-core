using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using WorkflowCore.Interface;
using WorkflowCore.Sample17.Steps;

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
                    .CompensateWith(context => Console.WriteLine("undo hello"))
                .Saga(saga => saga
                    .StartWith(context => Console.WriteLine("1"))
                        .CompensateWith(context => Console.WriteLine("undo 1"))
                    .Then(context =>
                    {
                        Console.WriteLine("2");                        
                        throw new Exception("boo");
                        Console.WriteLine("2.5");
                    })
                        .CompensateWith<CustomMessage>(x => x.Input(step => step.Message, data => "undo 2"))
                    .Then(context => Console.WriteLine("3"))
                    )
                    //.CompensateWithSequence(comp => comp
                    //    .StartWith(ctx => Console.WriteLine("fail saga1"))
                    //    .Then(ctx => Console.WriteLine("fail saga2"))
                    //    )
                .OnError(Models.WorkflowErrorHandling.Retry, TimeSpan.FromSeconds(5))
                .Then(context => Console.WriteLine("end"));
        }
    }
}
