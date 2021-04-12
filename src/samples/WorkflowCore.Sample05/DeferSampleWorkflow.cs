using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Sample05.Steps;

namespace WorkflowCore.Sample05
{
    public class DeferSampleWorkflow : IWorkflow
    {
        public string Id => "DeferSampleWorkflow";
            
        public int Version => 1;
            
        public void Build(IWorkflowBuilder<object> builder)
        {
            builder
                .StartWith(context =>
                {
                    Console.WriteLine("Workflow started");
                    return ExecutionResult.Next();
                })
                .Then<SleepStep>()
                    .Input(step => step.Period, data => TimeSpan.FromSeconds(20))
                .Then(context =>
                {
                    Console.WriteLine("workflow complete");
                    return ExecutionResult.Next();
                });
        }
    }
}
