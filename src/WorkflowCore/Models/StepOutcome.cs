using System;
using System.Linq.Expressions;

namespace WorkflowCore.Models
{
    public class StepOutcome
    {
        private LambdaExpression _value;

        public LambdaExpression Value
        {
            set { _value = value; }
        }
        
        public int NextStep { get; set; }

        public string Label { get; set; }

        public string ExternalNextStepId { get; set; }

        public object GetValue(object data)
        {
            if (_value == null)
                return null;

            return _value.Compile().DynamicInvoke(data);
        }
    }
}
