using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample19
{
    class TryCatchWorkflow : IWorkflow<TryCatchWorkflow.Data>
    {
        public string Id => "try-catch-sample";
        public int Version => 1;

        public void Build(IWorkflowBuilder<Data> builder)
        {
            builder.StartWith(_ => ExecutionResult.Next())
                .Output(data => data.Message, step => "Custom Message")
                .Try(b => b.StartWith(_ => throw new ArgumentException("I am Exception1")))
            .Catch(new[] {typeof(Exception)}, ctx =>
            {
                Console.WriteLine($"Caught an exception: Type: '{ctx.CurrentException.GetType().Name}', Message: '{ctx.CurrentException.Message}'");
                return ExecutionResult.Next();
            })
            .Then<CustomMessage>(s => s.Input(msg => msg.Message, data => data.Message));
        }

        public class Data
        {
            public string Message { get; set; }
        }
    }
}
