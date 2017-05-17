﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class StepOutcome
    {
        public Expression<Func<object, object>> Value { get; set; }
        
        public int NextStep { get; set; }

        public string Label { get; set; }

        public object GetValue(object data)
        {
            return Value.Compile().Invoke(data);
        }
    }
}
