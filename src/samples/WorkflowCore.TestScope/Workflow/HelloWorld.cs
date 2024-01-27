using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.TestScope.Workflow
{
    public class HelloWorld : StepBody
    {
        private readonly CountService _countService;

        public HelloWorld(CountService countService)
        {
            _countService = countService;
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            _countService.Increment();
            Console.WriteLine($"[{_countService.Count}] Hello world");
            return ExecutionResult.Next();
        }
    }
}
