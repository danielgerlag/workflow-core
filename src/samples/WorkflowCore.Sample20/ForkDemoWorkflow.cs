using System;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Sample20.Steps;

namespace WorkflowCore.Sample20
{
    public class ForkDemoWorkflow : IWorkflow<BatchData>
    {
        public string Id => "fork-demo";
        public int Version => 1;

        public void Build(IWorkflowBuilder<BatchData> builder)
        {
            builder
                .StartWith<ProcessBatch>()
                .WaitFor("ForkDecision", data => data.EventKey, data => DateTime.Now)
                .Then<ProcessBatch>()
                .Then(context =>
                {
                    var data = (BatchData)context.Workflow.Data;
                    Console.WriteLine($"Workflow {context.Workflow.Id} complete with {data.ProcessedCount} item(s).");
                });
        }
    }

    public class BatchData
    {
        public List<int> Items { get; set; } = new List<int>();
        public int Threshold { get; set; } = 5;
        public int ProcessedCount { get; set; }
        public string EventKey { get; set; }
    }
}
