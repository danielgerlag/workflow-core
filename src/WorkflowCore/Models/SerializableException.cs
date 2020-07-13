using System;

namespace WorkflowCore.Models
{
    public class SerializableException
    {
        public string FullTypeName { get; private set; }
        
        public string Message { get; private set; }
        
        public string StackTrace { get; private set; }

        public SerializableException(Exception exception)
        {
            FullTypeName = exception.GetType().FullName;
            Message = exception.Message;
            StackTrace = exception.StackTrace;
        }
    }
}