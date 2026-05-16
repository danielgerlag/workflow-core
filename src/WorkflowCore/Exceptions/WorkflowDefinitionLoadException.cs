using System;

namespace WorkflowCore.Exceptions
{
    public class WorkflowDefinitionLoadException : Exception
    {
        public WorkflowDefinitionLoadException(string message)
            : base (message)
        {            
        }

        public WorkflowDefinitionLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
