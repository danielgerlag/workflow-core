using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Exceptions
{
    public class WorkflowNotRegisteredException : Exception
    {
        public WorkflowNotRegisteredException(string workflowId, int? version)
            : base($"Workflow {workflowId} {version} is not registered")
        {
        }
    }
}
