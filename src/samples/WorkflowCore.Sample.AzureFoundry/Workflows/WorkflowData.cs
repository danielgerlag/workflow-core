namespace WorkflowCore.Sample.AzureFoundry.Workflows
{
    /// <summary>
    /// Data for simple chat workflow
    /// </summary>
    public class ChatWorkflowData
    {
        public string UserMessage { get; set; }
        public string Response { get; set; }
        public int TokensUsed { get; set; }
    }

    /// <summary>
    /// Data for agent with tools workflow
    /// </summary>
    public class AgentWorkflowData
    {
        public string UserRequest { get; set; }
        public string AgentResponse { get; set; }
        public int IterationsUsed { get; set; }
    }

    /// <summary>
    /// Data for human review workflow
    /// </summary>
    public class ReviewWorkflowData
    {
        public string Topic { get; set; }
        public string Reviewer { get; set; }
        public string GeneratedContent { get; set; }
        public string ApprovedContent { get; set; }
        public bool IsApproved { get; set; }
        
        /// <summary>
        /// Optional custom correlation ID for the review.
        /// If provided, this will be used as the event key.
        /// If not provided, the workflow ID will be used.
        /// </summary>
        public string ReviewId { get; set; }
        
        /// <summary>
        /// The event key to use when completing the review.
        /// Use this with: workflowHost.PublishEvent("HumanReview", eventKey, reviewAction)
        /// </summary>
        public string EventKey { get; set; }
    }
}
