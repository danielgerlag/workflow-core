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
                .Try(b => b.StartWith(_ => ExecutionResult.Next())
                    .Try(b2 => b2.StartWith(_ => throw new Exception("I am Exception1")))
                    .Catch(new []{typeof(ApplicationException)}, ctx =>
                    {
                        Console.WriteLine(
                            $"Caught an exception in inner catch: Type: '{ctx.CurrentException.FullTypeName}', Message: '{ctx.CurrentException.Message}'");
                        return ExecutionResult.Next();
                    }))
                .Catch(new []{typeof(Exception)}, ctx =>
                {
                    Console.WriteLine(
                        $"Caught an exception in outer catch: Type: '{ctx.CurrentException.FullTypeName}', Message: '{ctx.CurrentException.Message}'");
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