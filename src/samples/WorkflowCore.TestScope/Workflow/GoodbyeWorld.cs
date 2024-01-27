using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.TestScope.Workflow
{
    public class GoodbyeWorld : StepBody
    {
        private readonly CountService _countService;

        public GoodbyeWorld(CountService countService)
        {
            _countService = countService;
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            _countService.Increment();
            Console.WriteLine($"[{_countService.Count}] Goodbye world");
            return ExecutionResult.Next();
        }
    }
}
