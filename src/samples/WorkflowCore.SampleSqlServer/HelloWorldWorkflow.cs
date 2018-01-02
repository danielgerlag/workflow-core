#region using

using System;
using System.Linq;

using WorkflowCore.Interface;
using WorkflowCore.SampleSqlServer.Steps;

#endregion

namespace WorkflowCore.SampleSqlServer
{
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder<object> builder)
        {
            var stepBuilder = builder
                .StartWith<HelloWorld>()
                .Delay(o => TimeSpan.FromSeconds(2))
                .Then<GoodbyeWorld>();
        }

        public string Id => "HelloWorld";

        public int Version => 1;
    }
}