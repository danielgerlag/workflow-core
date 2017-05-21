using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;

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

        public object GetValue(object data)
        {
            if (_value == null)
                return null;

            return _value.Compile().Invoke(data);
        }
    }
}
