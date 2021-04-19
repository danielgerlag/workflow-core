using System;
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
                .StartWith(context => Console.WriteLine("Begin"))
                .Saga(saga => saga
                    .StartWith<Task1>()
                        .CompensateWith<UndoTask1>()
                    .Then<Task2>()
                        .CompensateWith<UndoTask2>()
                    .Then<Task3>()
                        .CompensateWith<UndoTask3>()
                )
                    .OnError(Models.WorkflowErrorHandling.Retry, TimeSpan.FromSeconds(5))
                .Then(context => Console.WriteLine("End"));
        }
    }
}
