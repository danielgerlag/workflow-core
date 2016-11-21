using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowBuilder : IWorkflowBuilder
    {
        public int InitialStep { get; set; }

        protected List<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();

        public IWorkflowBuilder<T> UseData<T>()
        {
            IWorkflowBuilder<T> result = new WorkflowBuilder<T>(Steps);
            result.InitialStep = this.InitialStep;            
            return result;
        }
        

        public WorkflowDefinition Build(string id, int version)
        {
            WorkflowDefinition result = new WorkflowDefinition();
            result.Id = id;
            result.Version = version;
            result.Steps = this.Steps;            
            return result;
        }

        public void AddStep(WorkflowStep step)
        {
            step.Id = Steps.Count();
            Steps.Add(step);
        }
        
    }

    public class WorkflowBuilder<TData> : WorkflowBuilder, IWorkflowBuilder<TData>
    {        

        public WorkflowBuilder(IEnumerable<WorkflowStep> steps)
        {
            this.Steps.AddRange(steps);
        }

        public IStepBuilder<TData, TStep> StartWith<TStep>(Action<IStepBuilder<TData, TStep>> stepSetup = null)
            where TStep : IStepBody
        {
            WorkflowStep<TStep> step = new WorkflowStep<TStep>();
            var stepBuilder = new StepBuilder<TData, TStep>(this, step);

            if (stepSetup != null)
                stepSetup.Invoke(stepBuilder);

            step.Name = step.Name ?? typeof(TStep).Name;
            AddStep(step);
            this.InitialStep = step.Id;
            return stepBuilder;
        }

        public IStepBuilder<TData, InlineStepBody> StartWith(Func<IStepExecutionContext, ExecutionResult> body)
        {
            WorkflowStepInline newStep = new WorkflowStepInline();
            newStep.Body = body;
            var stepBuilder = new StepBuilder<TData, InlineStepBody>(this, newStep);
            AddStep(newStep);
            this.InitialStep = newStep.Id;
            return stepBuilder;
        }

        public IEnumerable<WorkflowStep> GetUpstreamSteps(int id)
        {
            return Steps.Where(x => x.Outcomes.Any(y => y.NextStep == id)).ToList();
        }
    }
        
}
