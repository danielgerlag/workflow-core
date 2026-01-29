using System;
using WorkflowCore.AI.AzureFoundry.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// Step body for human review of LLM output.
    /// 
    /// To complete a review, publish an event with:
    /// - EventName: "HumanReview"
    /// - EventKey: The value from the EventKey output property (or your custom CorrelationId if provided)
    /// - EventData: A ReviewAction object
    /// </summary>
    public class HumanReview : StepBody
    {
        public const string EventName = "HumanReview";
        public const string ExtContent = "ContentToReview";
        public const string ExtReviewer = "Reviewer";
        public const string ExtPrompt = "ReviewPrompt";
        public const string ExtEventKey = "EventKey";

        /// <summary>
        /// Content to be reviewed
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Principal/user assigned to review
        /// </summary>
        public string Reviewer { get; set; }

        /// <summary>
        /// Prompt/instructions for the reviewer
        /// </summary>
        public string ReviewPrompt { get; set; }

        /// <summary>
        /// Optional custom correlation ID for the event key.
        /// If not provided, defaults to "{workflowId}".
        /// Use this to correlate reviews with external systems (e.g., ticket ID, request ID).
        /// </summary>
        public string CorrelationId { get; set; }

        // Outputs

        /// <summary>
        /// The event key to use when publishing the review decision.
        /// Store this value to later complete the review via workflowHost.PublishEvent().
        /// </summary>
        public string EventKey { get; set; }

        /// <summary>
        /// The review action taken
        /// </summary>
        public ReviewAction ReviewAction { get; set; }

        /// <summary>
        /// The final approved content (original or modified)
        /// </summary>
        public string ApprovedContent { get; set; }

        /// <summary>
        /// The decision made by the reviewer
        /// </summary>
        public ReviewDecision Decision { get; set; }

        /// <summary>
        /// Whether the content was approved (Approved or ApprovedWithChanges)
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Comments from the reviewer
        /// </summary>
        public string Comments { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (!context.ExecutionPointer.EventPublished)
            {
                // Generate the event key - use custom CorrelationId if provided, otherwise use workflowId
                EventKey = !string.IsNullOrEmpty(CorrelationId) 
                    ? CorrelationId 
                    : context.Workflow.Id;

                context.ExecutionPointer.ExtensionAttributes[ExtContent] = Content;
                context.ExecutionPointer.ExtensionAttributes[ExtReviewer] = Reviewer;
                context.ExecutionPointer.ExtensionAttributes[ExtPrompt] = ReviewPrompt;
                context.ExecutionPointer.ExtensionAttributes[ExtEventKey] = EventKey;

                var effectiveDate = DateTime.UtcNow;

                return ExecutionResult.WaitForEvent(EventName, EventKey, effectiveDate);
            }

            // Restore EventKey from extension attributes for output
            if (context.ExecutionPointer.ExtensionAttributes.TryGetValue(ExtEventKey, out var storedKey))
            {
                EventKey = storedKey?.ToString();
            }

            if (!(context.ExecutionPointer.EventData is ReviewAction action))
            {
                throw new InvalidOperationException("Expected ReviewAction event data");
            }

            ReviewAction = action;
            Decision = action.Decision;
            Comments = action.Comments;

            switch (action.Decision)
            {
                case ReviewDecision.Approved:
                    ApprovedContent = Content;
                    IsApproved = true;
                    break;

                case ReviewDecision.ApprovedWithChanges:
                    ApprovedContent = action.ModifiedContent ?? Content;
                    IsApproved = true;
                    break;

                case ReviewDecision.Rejected:
                case ReviewDecision.Regenerate:
                    ApprovedContent = null;
                    IsApproved = false;
                    break;
            }

            context.ExecutionPointer.ExtensionAttributes["ReviewDecision"] = action.Decision.ToString();
            context.ExecutionPointer.ExtensionAttributes["ReviewedBy"] = action.Reviewer;

            return ExecutionResult.Next();
        }
    }
}
