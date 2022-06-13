using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WebApiSample.Steps
{
    public class DSLHelloWorldStep : StepBody
    {
        public JObject Home { get; set; }

        public string Result { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var home = Home.ToObject<Dictionary<string, string>>();

            Result = string.Join(";", home.Select(x => x.Key + "=" + x.Value).ToArray());

            return ExecutionResult.Next();
        }
    }
}
