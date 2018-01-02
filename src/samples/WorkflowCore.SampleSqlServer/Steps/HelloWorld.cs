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
            Console.WriteLine("Hello world");

            return ExecutionResult.Next();
        }
    }
}