#region using

using System;
using System.Linq;

using WorkflowCore.Interface;
using WorkflowCore.Models;

#endregion

namespace WorkflowCore.SampleSqlServer.Steps
{
    public class HelloWorld : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (HelloWorldData)context.Workflow.Data;

            Console.WriteLine("Hello world " + data.ID);

            data.ID++;

            return ExecutionResult.Next();
        }
    }
}