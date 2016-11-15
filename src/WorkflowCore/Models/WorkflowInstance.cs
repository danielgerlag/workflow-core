using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class WorkflowInstance
    {
        public string Id { get; set; }

        //public WorkflowDefinition Definition { get; set; }

        public string WorkflowDefinitionId { get; set; }

        public int Version { get; set; }

        public string Description { get; set; }

        public List<ExecutionPointer> ExecutionPointers { get; set; } = new List<ExecutionPointer>();

        public long? NextExecution { get; set; }

        public object Data { get; set; }        
               
    }        
}
