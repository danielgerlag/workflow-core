using System;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Sample15.Services;

namespace WorkflowCore.Sample15.Steps
{
    public class DoSomething : StepBody
    {
        private IMyService _myService;

        public DoSomething(IMyService myService)
        {
            _myService = myService;
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            _myService.DoTheThings();
            return ExecutionResult.Next();
        }
    }
}
