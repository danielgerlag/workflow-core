using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public abstract class WorkflowStep
    {
        public abstract Type BodyType { get; }

        public int Id { get; set; }

        public string Name { get; set; }

        public List<StepOutcome> Outcomes { get; set; } = new List<StepOutcome>();

        public List<DataMapping> Inputs { get; set; } = new List<DataMapping>();
        public List<DataMapping> Outputs { get; set; } = new List<DataMapping>();
    }

    public class WorkflowStep<TStepBody> : WorkflowStep
        where TStepBody : IStepBody 
    {
        public override Type BodyType
        {
            get { return typeof(TStepBody); }
        }
                                
    }

    


}
