using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace WorkflowCore.Models
{
    public class DataMapping
    {        
        public LambdaExpression Source { get; set; }
                
        public LambdaExpression Target { get; set; }
    }
}
