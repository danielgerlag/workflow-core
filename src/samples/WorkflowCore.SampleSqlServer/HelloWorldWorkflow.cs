#region using

using System;
using System.Linq;

using WorkflowCore.Interface;
using WorkflowCore.SampleSqlServer.Steps;

#endregion

namespace WorkflowCore.SampleSqlServer
{
    public class HelloWorldData
    {
        public int ID { get; set; }
        public long EventId { get; set; }
    }

    public class HelloWorldWorkflow : IWorkflow<HelloWorldData>
    {
        public void Build(IWorkflowBuilder<HelloWorldData> builder)
        {
            var stepBuilder = builder
                .StartWith<HelloWorld>()
                .WaitFor("Go", data => "0")
                    .Output(data => data.EventId, step => step.EventData)
                .Then<GoodbyeWorld>()
                .EndWorkflow();
        }

        public string Id => "HelloWorld";

        public int Version => 1;
    }
}