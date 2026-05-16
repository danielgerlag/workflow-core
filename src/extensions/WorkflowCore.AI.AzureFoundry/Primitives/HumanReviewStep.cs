using System;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// WorkflowStep wrapper for HumanReview
    /// </summary>
    public class HumanReviewStep : WorkflowStep<HumanReview>
    {
        public override Type BodyType => typeof(HumanReview);
    }
}
