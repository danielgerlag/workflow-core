using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Models.DefinitionStorage.v1
{
    public class StepSourceV1
    {
        public string StepType { get; set; }

        public string BodyType { get; set; }

        public string Id { get; set; }

        public string Name { get; set; }

        public WorkflowErrorHandling? ErrorBehavior { get; set; }

        public TimeSpan? RetryInterval { get; set; }

        public List<StepSourceV1> Do { get; set; } = new List<StepSourceV1>();

        public string NextStepId { get; set; }

        public List<MappingSourceV1> Inputs { get; set; } = new List<MappingSourceV1>();

        public List<MappingSourceV1> Outputs { get; set; } = new List<MappingSourceV1>();

        
    }
}
