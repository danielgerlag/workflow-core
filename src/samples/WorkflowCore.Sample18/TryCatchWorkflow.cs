using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample18
{
    class TryCatchWorkflow : IWorkflow
    {
        public string Id => "try-catch-sample";
        public int Version => 1;

        public void Build(IWorkflowBuilder<object> builder)
        { 
            builder.StartWith(_ => ExecutionResult.Next())
                .Try(b => b.StartWith(_ => throw new Exception("asdf")))
                .Catch(new[] {typeof(Exception)},
                    ctx => Console.WriteLine("FFFFFF " + ctx.CurrentException.Message));
        }
    }
}
