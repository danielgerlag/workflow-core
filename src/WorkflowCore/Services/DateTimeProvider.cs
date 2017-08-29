using System;
using WorkflowCore.Interface;

namespace WorkflowCore.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
