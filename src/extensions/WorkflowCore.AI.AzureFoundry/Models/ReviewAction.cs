using System;

namespace WorkflowCore.AI.AzureFoundry.Models
{
    /// <summary>
    /// Represents a human review action on LLM output
    /// </summary>
    public class ReviewAction
    {
        /// <summary>
        /// The decision made by the reviewer
        /// </summary>
        public ReviewDecision Decision { get; set; }

        /// <summary>
        /// The reviewer's identity
        /// </summary>
        public string Reviewer { get; set; }

        /// <summary>
        /// Modified content (if the reviewer edited the original)
        /// </summary>
        public string ModifiedContent { get; set; }

        /// <summary>
        /// Comments from the reviewer
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// When the review was completed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Possible decisions for human review
    /// </summary>
    public enum ReviewDecision
    {
        /// <summary>
        /// Content approved as-is
        /// </summary>
        Approved,

        /// <summary>
        /// Content approved with modifications
        /// </summary>
        ApprovedWithChanges,

        /// <summary>
        /// Content rejected
        /// </summary>
        Rejected,

        /// <summary>
        /// Request regeneration from LLM
        /// </summary>
        Regenerate
    }
}
