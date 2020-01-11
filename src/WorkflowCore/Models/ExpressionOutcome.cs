using System;
using System.Linq.Expressions;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class ExpressionOutcome<TData> : IStepOutcome
    {
        private readonly Func<TData, object, bool> _func;

        
        public int NextStep { get; set; }

        public string Label { get; set; }

        public string ExternalNextStepId { get; set; }

        public ExpressionOutcome(Expression<Func<TData, object, bool>> expression)
        {
            _func = expression.Compile();
        }

        public bool Matches(ExecutionResult executionResult, object data)
        {
            return _func((TData)data, executionResult.OutcomeValue);
        }

        public bool Matches(object data)
        {
            return _func((TData)data, null);
        }
                
    }
}
