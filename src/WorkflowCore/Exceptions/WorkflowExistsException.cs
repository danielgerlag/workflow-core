using System;
using System.Runtime.Serialization;

namespace WorkflowCore.Exceptions
{
    public class WorkflowExistsException : Exception
    {
        public WorkflowExistsException()
            : base("Workflow with provided correlation id already exists.")
        {
        }

        protected WorkflowExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public WorkflowExistsException(Exception innerException)
            : base("Workflow with provided correlation id already exists.", innerException)
        {
        }
    }
}
