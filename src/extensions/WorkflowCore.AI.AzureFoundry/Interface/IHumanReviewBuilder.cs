using System;
using System.Linq.Expressions;
using WorkflowCore.AI.AzureFoundry.Models;
using WorkflowCore.AI.AzureFoundry.Primitives;
using WorkflowCore.Interface;

namespace WorkflowCore.AI.AzureFoundry.Interface
{
    /// <summary>
    /// Builder interface for configuring HumanReview steps
    /// </summary>
    public interface IHumanReviewBuilder<TData> : IStepBuilder<TData, HumanReview>
    {
        /// <summary>
        /// Set the content to be reviewed
        /// </summary>
        IHumanReviewBuilder<TData> Content(Expression<Func<TData, string>> expression);

        /// <summary>
        /// Set the reviewer
        /// </summary>
        IHumanReviewBuilder<TData> Reviewer(Expression<Func<TData, string>> expression);

        /// <summary>
        /// Set the review prompt/instructions
        /// </summary>
        IHumanReviewBuilder<TData> Prompt(string prompt);

        /// <summary>
        /// Set a custom correlation ID for the event key.
        /// This allows you to use a known value (e.g., ticket ID, request ID) 
        /// to later complete the review via PublishEvent.
        /// If not set, defaults to the workflow ID.
        /// </summary>
        IHumanReviewBuilder<TData> CorrelationId(Expression<Func<TData, string>> expression);

        /// <summary>
        /// Output the event key to workflow data.
        /// Use this value to later complete the review via:
        /// workflowHost.PublishEvent("HumanReview", eventKey, reviewAction)
        /// </summary>
        IHumanReviewBuilder<TData> OnEventKey(Expression<Func<TData, string>> expression);

        /// <summary>
        /// Output the approved content to workflow data
        /// </summary>
        IHumanReviewBuilder<TData> OnApproved(Expression<Func<TData, string>> expression);

        /// <summary>
        /// Output the decision to workflow data
        /// </summary>
        IHumanReviewBuilder<TData> OutputDecisionTo(Expression<Func<TData, ReviewDecision>> expression);
    }
}
