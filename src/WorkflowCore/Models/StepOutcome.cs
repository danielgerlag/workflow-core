using System;
using System.Linq.Expressions;

namespace WorkflowCore.Models
{
    public class StepOutcome
    {
        private Expression<Func<object, object>> _value;

        public Expression<Func<object, object>> Value
        {
            set { _value = value; }
        }
        
        public int NextStep { get; set; }

        public string Label { get; set; }

        public string Tag { get; set; }

        public object GetValue(object data)
        {
            if (_value == null)
                return null;

            return _value.Compile().Invoke(data);
        }
    }
}
