using System.Linq.Expressions;

namespace WorkflowCore.Models
{
    public class DataMapping
    {        
        public LambdaExpression Source { get; set; }
                
        public LambdaExpression Target { get; set; }
    }
}
