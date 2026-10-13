using WorkflowCore.AI;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WorkflowCoreAIServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="ChatClientStep"/>. An <c>IChatClient</c> must also be registered.
        /// </summary>
        public static IServiceCollection AddWorkflowChatClientStep(this IServiceCollection services)
        {
            services.AddTransient<ChatClientStep>();
            return services;
        }
    }
}
