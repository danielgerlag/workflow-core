using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Primitives;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample.AzureFoundry.Workflows
{
    /// <summary>
    /// Workflow demonstrating human-in-the-loop review of AI-generated content
    /// </summary>
    public class HumanReviewWorkflow : IWorkflow<ReviewWorkflowData>
    {
        public string Id => "HumanReviewWorkflow";
        public int Version => 1;

        public void Build(IWorkflowBuilder<ReviewWorkflowData> builder)
        {
            builder
                .StartWith(context => ExecutionResult.Next())
                
                // Step 1: Generate content with AI
                .ChatCompletion(cfg => cfg
                    .SystemPrompt("You are a content writer. Write clear, engaging content on the given topic.")
                    .UserMessage(data => $"Write a short paragraph about: {data.Topic}")
                    .MaxTokens(300)
                    .OutputTo(data => data.GeneratedContent))
                
                // Step 2: Wait for human review
                // Use CorrelationId to provide a known event key for completing the review
                // If not provided, defaults to the workflowId
                .HumanReview(cfg => cfg
                    .Content(data => data.GeneratedContent)
                    .Reviewer(data => data.Reviewer)
                    .Prompt("Please review this AI-generated content. Approve, modify, or reject.")
                    .CorrelationId(data => data.ReviewId)  // Use custom correlation ID if provided
                    .OnApproved(data => data.ApprovedContent)
                    .OnEventKey(data => data.EventKey));  // Capture the event key for completing the review
        }
    }
}
