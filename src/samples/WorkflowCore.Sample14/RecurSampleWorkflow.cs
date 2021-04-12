using System;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample14
{
    class RecurSampleWorkflow : IWorkflow<MyData>
    {
        public string Id => "recur-sample";
        public int Version => 1;

        public void Build(IWorkflowBuilder<MyData> builder)
        {
            builder
                .StartWith(context => Console.WriteLine("Hello"))
                .Recur(data => TimeSpan.FromSeconds(5), data => data.Counter > 5).Do(recur => recur
                    .StartWith(context => Console.WriteLine("Doing recurring task"))
                )
                .Then(context => Console.WriteLine("Carry on"));
        }
    }

    public class MyData
    {
        public int Counter { get; set; }
    }
}
