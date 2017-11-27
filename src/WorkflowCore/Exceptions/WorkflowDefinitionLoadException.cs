using System;
using System.Collections.Generic;
using System.Text;

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
