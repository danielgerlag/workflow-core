namespace WorkflowCore.Providers.Azure.Services
{
    public sealed class CosmosDbStorageOptions
    {
        /// <summary>
        /// The default name of workflow container.
        /// </summary>
        public const string DefaultWorkflowContainerName = "workflows";

        /// <summary>
        /// The name of Workflow container in Cosmos DB.
        /// </summary>
        public string WorkflowContainerName { get; set; } = DefaultWorkflowContainerName;

        /// <summary>
        /// The default name of event container.
        /// </summary>
        public const string DefaultEventContainerName = "events";

        /// <summary>
        /// The name of Event container in Cosmos DB.
        /// </summary>
        public string EventContainerName { get; set; } = DefaultEventContainerName;

        /// <summary>
        /// The default name of subscription container.
        /// </summary>
        public const string DefaultSubscriptionContainerName = "subscriptions";

        /// <summary>
        /// The name of Subscription container in Cosmos DB.
        /// </summary>
        public string SubscriptionContainerName { get; set; } = DefaultSubscriptionContainerName;
    }
}
