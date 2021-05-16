using System;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample16
{    
    class ScheduleWorkflow : IWorkflow
    {
        public string Id => "schedule-sample";
        public int Version => 1;

        public void Build(IWorkflowBuilder<object> builder)
        {
            builder
                .StartWith(context => Console.WriteLine("Hello"))
                .Schedule(data => TimeSpan.FromSeconds(5)).Do(schedule => schedule
                    .StartWith(context => Console.WriteLine("Doing scheduled tasks"))
                )
                .Then(context => Console.WriteLine("Doing normal tasks"));
        }
    }
}
