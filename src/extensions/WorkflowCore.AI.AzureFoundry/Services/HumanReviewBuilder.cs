using System;
using System.Linq.Expressions;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;
using WorkflowCore.AI.AzureFoundry.Primitives;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;

namespace WorkflowCore.AI.AzureFoundry.Services
{
    /// <summary>
    /// Builder for HumanReview steps
    /// </summary>
    public class HumanReviewBuilder<TData> : StepBuilder<TData, HumanReview>, IHumanReviewBuilder<TData>
    {
        public HumanReviewBuilder(IWorkflowBuilder<TData> workflowBuilder, WorkflowStep<HumanReview> step)
            : base(workflowBuilder, step)
        {
        }

        public IHumanReviewBuilder<TData> Content(Expression<Func<TData, string>> expression)
        {
            Input(s => s.Content, expression);
            return this;
        }

        public IHumanReviewBuilder<TData> Reviewer(Expression<Func<TData, string>> expression)
        {
            Input(s => s.Reviewer, expression);
            return this;
        }

        public IHumanReviewBuilder<TData> Prompt(string prompt)
        {
            Input(s => s.ReviewPrompt, d => prompt);
            return this;
        }

        public IHumanReviewBuilder<TData> CorrelationId(Expression<Func<TData, string>> expression)
        {
            Input(s => s.CorrelationId, expression);
            return this;
        }

        public IHumanReviewBuilder<TData> OnEventKey(Expression<Func<TData, string>> expression)
        {
            Output(expression, s => s.EventKey);
            return this;
        }

        public IHumanReviewBuilder<TData> OnApproved(Expression<Func<TData, string>> expression)
        {
            Output(expression, s => s.ApprovedContent);
            return this;
        }

        public IHumanReviewBuilder<TData> OutputDecisionTo(Expression<Func<TData, ReviewDecision>> expression)
        {
            Output(expression, s => s.Decision);
            return this;
        }
    }
}
