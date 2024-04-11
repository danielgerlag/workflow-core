using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.TestScope.Workflow
{
    public class GoodbyeWorldWorkflow : IWorkflow<WorkflowData>
    {
        public string Id => "GoodbyeWorld";
        public int Version => 1;

        public void Build(IWorkflowBuilder<WorkflowData> builder)
        {
            builder
                .StartWith(_ => Console.WriteLine("GoodbyeWorld started"))
                .ForEach(data => Enumerable.Range(1, data.ExecuteTimes))
                    .Do(x => x.StartWith<GoodbyeWorld>());
        }
    }
}
