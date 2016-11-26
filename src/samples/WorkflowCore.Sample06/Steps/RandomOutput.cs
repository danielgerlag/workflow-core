using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample06.Steps
{
    public class RandomOutput : StepBody
    {        
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Random rnd = new Random();
            int value = rnd.Next(2);
            Console.WriteLine("Generated random value {0}", value);
            return ExecutionResult.Outcome(value);
        }
    }
}
