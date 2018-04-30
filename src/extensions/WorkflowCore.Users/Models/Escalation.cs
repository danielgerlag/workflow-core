using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace WorkflowCore.Users.Models
{
    public class Escalation<TData>
    {
        public Expression<Func<TData, TimeSpan>> TimeOut { get; set; }

        public Expression<Func<TData, string>> NewUser { get; set; }
    }
}