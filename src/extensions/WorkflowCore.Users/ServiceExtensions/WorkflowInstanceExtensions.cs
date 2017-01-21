using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using WorkflowCore.Users.Models;

namespace WorkflowCore.Models
{
    public static class WorkflowInstanceExtensions
    {
        public static IEnumerable<OpenUserAction> GetOpenUserActions(this WorkflowInstance workflow)
        {
            throw new NotImplementedException();
        }
    }
}
