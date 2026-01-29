namespace WorkflowCore.AI.AzureFoundry.Models
{
    /// <summary>
    /// Result from a chat completion request
    /// </summary>
    public class ChatCompletionResult
    {
        /// <summary>
        /// The generated response text
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// Reason the completion finished
        /// </summary>
        public string FinishReason { get; set; }

        /// <summary>
        /// Number of tokens in the prompt
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// Number of tokens in the completion
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Total tokens used
        /// </summary>
        public int TotalTokens => PromptTokens + CompletionTokens;

        /// <summary>
        /// Model used for the completion
        /// </summary>
        public string Model { get; set; }
    }
}
