using System;
using System.Collections.Generic;
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

        public string NextStepId { get; set; }

        public Dictionary<string, string> Inputs { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> Outputs { get; set; } = new Dictionary<string, string>();

        
    }
}
