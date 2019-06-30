using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace WorkflowCore.Models.DefinitionStorage.v1
{
    public class StepSourceV1
    {
        public string StepType { get; set; }
        
        public string Id { get; set; }

        public string Name { get; set; }

        public string CancelCondition { get; set; }

        public WorkflowErrorHandling? ErrorBehavior { get; set; }

        public TimeSpan? RetryInterval { get; set; }

        public List<List<StepSourceV1>> Do { get; set; } = new List<List<StepSourceV1>>();

        public List<StepSourceV1> CompensateWith { get; set; } = new List<StepSourceV1>();

        public bool Saga { get; set; } = false;

        public string NextStepId { get; set; }

        public ExpandoObject Inputs { get; set; } = new ExpandoObject();

        public Dictionary<string, string> Outputs { get; set; } = new Dictionary<string, string>();

        
    }
}
