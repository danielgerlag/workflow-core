using System;

namespace WorkflowCore.Exceptions
{
    public class WorkflowDefinitionLoadException : Exception
    {
        public WorkflowDefinitionLoadException(string message)
            : base (message)
        {            
        }
    }
}
