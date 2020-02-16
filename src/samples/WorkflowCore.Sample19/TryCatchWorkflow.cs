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
            // builder.StartWith(_ => ExecutionResult.Next())
            //     .Output(data => data.Message, step => "Custom Message")
            //     .Try(b => b.StartWith(_ => throw new AggregateException("I am Exception1")))
            //     .Catch(new[] {typeof(ArgumentException)},
            //         ctx => Console.WriteLine($"Caught ArgumentException, message: {ctx.CurrentException.Message}"))
            //     .Catch(new[] {typeof(Exception)},
            // ctx => Console.WriteLine($"Caught Exception, message: {ctx.CurrentException.Message}"))
            //     .Then<CustomMessage>(s => s.Input(msg => msg.Message, data => data.Message));

            builder.StartWith(_ => ExecutionResult.Next())
                .Output(data => data.Message, step => "Custom Message")
                .Try(b => b.StartWith(_ => throw new Exception("I am Exception1")))
                .Catch<CustomMessage>(new[] {typeof(Exception)},
                    s => s.Input(msg => msg.Message, data => data.Message));
        }

        public class Data
        {
            public string Message { get; set; }
        }
    }
}
