using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample09
{    
    public class ForEachWorkflow : IWorkflow
    {
        public string Id => "Foreach";

        public int Version => 1;

        public void Build(IWorkflowBuilder<object> builder)
        {
            builder
                .StartWith(context =>
                {
                    Console.WriteLine("Hello");
                    return ExecutionResult.Next();
                })
                .ForEach(data => new List<int>() { 1, 2, 3, 4 })
                    .Do(x => x
                        .StartWith(context =>
                        {
                            Console.WriteLine($"iteration {context.Item}");
                            return ExecutionResult.Next();
                        })
                        .Then(context =>
                        {
                            Console.WriteLine($"step2 {context.Item}");
                            return ExecutionResult.Next();
                        }))
                .Then(context => 
                {
                    Console.WriteLine("bye");
                    return ExecutionResult.Next();
                });
        }        
    }
}
