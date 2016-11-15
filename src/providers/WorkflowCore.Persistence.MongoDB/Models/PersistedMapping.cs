using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Persistence.MongoDB.Models
{
    public class PersistedMapping
    {
        public string SourceParameterType { get; set; }
        public string SourceExpression { get; set; }
        public string SourceReturnType { get; set; }

        public string TargetParameterType { get; set; }
        public string TargetExpression { get; set; }
        public string TargetReturnType { get; set; }
    }
}
