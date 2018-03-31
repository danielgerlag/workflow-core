using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowCore.Users.Models
{
    public class Escalation
    {
        public TimeSpan TimeOut { get; set; }

        public string NewUser { get; set; }
    }
}
