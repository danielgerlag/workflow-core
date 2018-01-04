#region using

using System;
using System.Linq;

using Microsoft.Extensions.Logging;

using WorkflowCore.Interface;
using WorkflowCore.Models;

#endregion

namespace WorkflowCore.SampleSqlServer.Steps
{
    public class GoodbyeWorld : StepBody
    {
        private readonly ILogger _logger;

        public GoodbyeWorld(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GoodbyeWorld>();
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var data = (HelloWorldData)context.Workflow.Data;

            Console.WriteLine("Goodbye world " + data.ID);
            _logger.LogInformation("Hi there!");

            return ExecutionResult.Next();
        }
    }
}