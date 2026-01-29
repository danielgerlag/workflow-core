using System;
using System.Threading.Tasks;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.ServiceExtensions;
using WorkflowCore.Interface;
using WorkflowCore.Sample.AzureFoundry.Tools;
using WorkflowCore.Sample.AzureFoundry.Workflows;

namespace WorkflowCore.Sample.AzureFoundry
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Load environment variables from .env file
            Env.Load();

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var serviceProvider = ConfigureServices(configuration);
            
            // Register tools
            var toolRegistry = serviceProvider.GetRequiredService<IToolRegistry>();
            toolRegistry.Register(serviceProvider.GetRequiredService<WeatherTool>());
            toolRegistry.Register(serviceProvider.GetRequiredService<CalculatorTool>());

            var host = serviceProvider.GetRequiredService<IWorkflowHost>();
            
            // Register workflows
            host.RegisterWorkflow<SimpleChatWorkflow, ChatWorkflowData>();
            host.RegisterWorkflow<AgentWithToolsWorkflow, AgentWorkflowData>();
            host.RegisterWorkflow<HumanReviewWorkflow, ReviewWorkflowData>();
            
            host.Start();

            Console.WriteLine("=== WorkflowCore Azure AI Foundry Sample ===");
            Console.WriteLine();
            Console.WriteLine("Choose a workflow to run:");
            Console.WriteLine("1. Simple Chat Completion");
            Console.WriteLine("2. Agent with Tools (Agentic Loop)");
            Console.WriteLine("3. Human-in-the-Loop Review");
            Console.WriteLine("Q. Quit");
            Console.WriteLine();

            while (true)
            {
                Console.Write("Enter choice: ");
                var choice = Console.ReadLine()?.Trim().ToUpper();

                switch (choice)
                {
                    case "1":
                        await RunSimpleChatWorkflow(host);
                        break;
                    case "2":
                        await RunAgentWithToolsWorkflow(host);
                        break;
                    case "3":
                        await RunHumanReviewWorkflow(host);
                        break;
                    case "Q":
                        host.Stop();
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Try again.");
                        break;
                }
            }
        }

        private static async Task RunSimpleChatWorkflow(IWorkflowHost host)
        {
            Console.WriteLine("Type 'quit' to exit the conversation.");
            Console.WriteLine();
            
            while (true)
            {
                Console.Write("You: ");
                var message = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(message) || message.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Exiting chat.");
                    break;
                }

                var data = new ChatWorkflowData
                {
                    UserMessage = message
                };

                var workflowId = await host.StartWorkflow("SimpleChatWorkflow", data);
                
                // Wait a bit for the workflow to complete
                await Task.Delay(5000);
                
                var instance = await host.PersistenceStore.GetWorkflowInstance(workflowId);
                var result = instance.Data as ChatWorkflowData;
                
                Console.WriteLine($"Assistant: {result?.Response ?? "Still processing..."}");
                Console.WriteLine();
            }
        }

        private static async Task RunAgentWithToolsWorkflow(IWorkflowHost host)
        {
            Console.WriteLine("Available tools: weather (get weather for a city), calculator (do math)");
            Console.WriteLine("Type 'quit' to exit the conversation.");
            Console.WriteLine();
            
            while (true)
            {
                Console.Write("You: ");
                var message = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(message) || message.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Exiting agent conversation.");
                    break;
                }

                var data = new AgentWorkflowData
                {
                    UserRequest = message
                };

                var workflowId = await host.StartWorkflow("AgentWithToolsWorkflow", data);
                
                // Wait for the agent loop to complete
                await Task.Delay(15000);
                
                var instance = await host.PersistenceStore.GetWorkflowInstance(workflowId);
                var result = instance.Data as AgentWorkflowData;
                
                Console.WriteLine($"Agent: {result?.AgentResponse ?? "Still processing..."}");
                Console.WriteLine();
            }
        }

        private static async Task RunHumanReviewWorkflow(IWorkflowHost host)
        {
            Console.Write("Enter content to generate and review: ");
            var topic = Console.ReadLine();

            var data = new ReviewWorkflowData
            {
                Topic = topic,
                Reviewer = "demo-user"
            };

            var workflowId = await host.StartWorkflow("HumanReviewWorkflow", data);
            Console.WriteLine($"Started workflow: {workflowId}");
            
            // Wait for AI to generate content
            await Task.Delay(5000);
            
            var instance = await host.PersistenceStore.GetWorkflowInstance(workflowId);
            var result = instance.Data as ReviewWorkflowData;
            
            Console.WriteLine();
            Console.WriteLine("=== Content Generated by AI ===");
            Console.WriteLine(result?.GeneratedContent ?? "Still generating...");
            Console.WriteLine("================================");
            Console.WriteLine();
            Console.WriteLine("To approve, publish a HumanReview event. For this demo, auto-approving...");
            
            // In a real app, this would come from a UI or API
            // For demo, we auto-approve
            await host.PublishEvent(
                "HumanReview",
                $"{workflowId}.{GetReviewPointerId(instance)}",
                new WorkflowCore.AI.AzureFoundry.Models.ReviewAction
                {
                    Decision = WorkflowCore.AI.AzureFoundry.Models.ReviewDecision.Approved,
                    Reviewer = "demo-user"
                });
            
            await Task.Delay(2000);
            
            instance = await host.PersistenceStore.GetWorkflowInstance(workflowId);
            result = instance.Data as ReviewWorkflowData;
            
            Console.WriteLine();
            Console.WriteLine($"Final approved content: {result?.ApprovedContent ?? "Pending..."}");
            Console.WriteLine();
        }

        private static string GetReviewPointerId(WorkflowCore.Models.WorkflowInstance instance)
        {
            string lastId = null;
            foreach (var pointer in instance.ExecutionPointers)
            {
                if (pointer.StepName == "HumanReview")
                    return pointer.Id;
                lastId = pointer.Id;
            }
            return lastId;
        }

        private static IServiceProvider ConfigureServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            services.AddWorkflow();
            
            // Configure Azure AI Foundry
            services.AddAzureFoundry(options =>
            {
                options.Endpoint = configuration["AZURE_AI_ENDPOINT"] 
                    ?? throw new InvalidOperationException("AZURE_AI_ENDPOINT not configured. Copy .env.example to .env and fill in values.");
                options.ApiKey = configuration["AZURE_AI_API_KEY"];
                options.ProjectName = configuration["AZURE_AI_PROJECT"] ?? "default";
                options.DefaultModel = configuration["AZURE_AI_DEFAULT_MODEL"] ?? "gpt-4o";
                options.DefaultEmbeddingModel = configuration["AZURE_AI_EMBEDDING_MODEL"] ?? "text-embedding-3-small";
                options.SearchEndpoint = configuration["AZURE_SEARCH_ENDPOINT"];
                options.SearchApiKey = configuration["AZURE_SEARCH_API_KEY"];
            });
            
            // Register tools
            services.AddTransient<WeatherTool>();
            services.AddTransient<CalculatorTool>();
            
            return services.BuildServiceProvider();
        }
    }
}
