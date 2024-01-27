using System;
using System.Linq;
using WorkflowCore.Interface;

namespace WorkflowCore.TestScope.Workflow
{
    public class HelloWorldWorkflow : IWorkflow<WorkflowData>
    {
        public string Id => "HelloWorld";
        public int Version => 1;

        public void Build(IWorkflowBuilder<WorkflowData> builder)
        {
            builder
                .StartWith(_ => Console.WriteLine("HelloWorld started"))
                .ForEach(data => Enumerable.Range(1, data.ExecuteTimes))
                    .Do(x => x.StartWith<HelloWorld>());
        }
    }
}
