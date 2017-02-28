using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

        public WorkflowErrorHandling? ErrorBehavior { get; set; }

        public TimeSpan? RetryInterval { get; set; }                

        public virtual ExecutionPipelineDirective InitForExecution(IWorkflowHost host, IPersistenceProvider persistenceStore, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
        {
            return ExecutionPipelineDirective.Next;
        }

        public virtual ExecutionPipelineDirective BeforeExecute(IWorkflowHost host, IPersistenceProvider persistenceStore, IStepExecutionContext context, ExecutionPointer executionPointer, IStepBody body)
        {
            return ExecutionPipelineDirective.Next;
        }

        public virtual void AfterExecute(IWorkflowHost host, IPersistenceProvider persistenceStore, IStepExecutionContext context, ExecutionResult result, ExecutionPointer executionPointer)
        {            
        }

        public virtual IStepBody ConstructBody(IServiceProvider serviceProvider)
        {
            IStepBody body = (serviceProvider.GetService(BodyType) as IStepBody);
            if (body == null)
            {
                var stepCtor = BodyType.GetConstructor(new Type[] { });
                if (stepCtor != null)
                    body = (stepCtor.Invoke(null) as IStepBody);
            }

            return body;
        }

    }

    public enum ExecutionPipelineDirective { Next = 0, Defer = 1 }

    public class WorkflowStep<TStepBody> : WorkflowStep
        where TStepBody : IStepBody 
    {
        public override Type BodyType
        {
            get { return typeof(TStepBody); }
        }
                                
    }

    


}
