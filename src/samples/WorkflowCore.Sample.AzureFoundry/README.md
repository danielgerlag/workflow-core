# WorkflowCore Azure AI Foundry Sample

This sample demonstrates how to use the **WorkflowCore.AI.AzureFoundry** extension to build AI-powered, agentic workflows.

## Features Demonstrated

1. **Simple Chat Completion** - Conversational LLM chat with persistent conversation
2. **Agent with Tools** - Autonomous agent that uses tools (weather, calculator) to answer questions
3. **Human-in-the-Loop Review** - AI generates content, human approves/modifies before continuing

## Prerequisites

- .NET 8.0 or later
- Azure AI Foundry resource with deployed models (e.g., gpt-4o)
- API Key from your Azure AI resource

## Setup

1. **Copy the environment file:**
   ```bash
   cp .env.example .env
   ```

2. **Edit `.env` with your Azure AI credentials:**
   ```bash
   AZURE_AI_ENDPOINT=https://your-resource.services.ai.azure.com
   AZURE_AI_API_KEY=your-api-key-here
   AZURE_AI_DEFAULT_MODEL=gpt-4o
   ```

   Get your endpoint and API key from the Azure Portal:
   - Navigate to your Azure AI Foundry resource
   - Go to **Keys and Endpoint**
   - Copy the endpoint and one of the keys

## Running the Sample

```bash
cd src/samples/WorkflowCore.Sample.AzureFoundry
dotnet run
```

You'll see an interactive menu:

```
=== WorkflowCore Azure AI Foundry Sample ===

Choose a workflow to run:
1. Simple Chat Completion
2. Agent with Tools (Agentic Loop)
3. Human-in-the-Loop Review
Q. Quit

Enter choice:
```

## Sample Workflows

### 1. Simple Chat Completion

A conversational chat loop where you can have a multi-turn conversation with the LLM.

```
Enter choice: 1
Type 'quit' to exit the conversation.

You: What is the capital of France?
Assistant: The capital of France is Paris.

You: What's the population?
Assistant: Paris has a population of approximately 2.1 million in the city proper...

You: quit
```

**Workflow code:**
```csharp
builder
    .StartWith(context => ExecutionResult.Next())
    .ChatCompletion(cfg => cfg
        .SystemPrompt("You are a helpful assistant")
        .UserMessage(data => data.UserMessage)
        .OutputTo(data => data.Response));
```

### 2. Agent with Tools (Agentic Loop)

An autonomous agent that can use tools to accomplish tasks. The agent decides when and how to use tools based on your request.

**Available tools:**
- `weather` - Get current weather for any city
- `calculator` - Perform mathematical calculations

```
Enter choice: 2
Available tools: weather (get weather for a city), calculator (do math)
Type 'quit' to exit the conversation.

You: What's the weather in Seattle?
Agent: The current weather in Seattle is partly cloudy with a temperature of 31°C (87°F) and a humidity of 84%.

You: What is 25 * 4 + 10?
Agent: 25 × 4 + 10 = 110

You: What's the weather in Tokyo and convert the temperature from Celsius to Fahrenheit
Agent: The weather in Tokyo is sunny with a temperature of 28°C. Converting to Fahrenheit: (28 × 9/5) + 32 = 82.4°F

You: quit
```

**How it works:**
1. You send a request
2. The LLM analyzes your request and decides which tool(s) to use
3. Tools are executed automatically
4. Results are fed back to the LLM
5. The LLM provides a final response using the tool results

**Workflow code:**
```csharp
builder
    .StartWith(context => ExecutionResult.Next())
    .AgentLoop(cfg => cfg
        .SystemPrompt(@"You are a helpful assistant with access to tools.
            Use the weather tool to get weather information.
            Use the calculator tool for math operations.
            Always explain what you're doing.")
        .Message(data => data.UserRequest)
        .WithTool("weather")
        .WithTool("calculator")
        .MaxIterations(5)
        .AutoExecuteTools(true)
        .OutputTo(data => data.AgentResponse));
```

### 3. Human-in-the-Loop Review

Demonstrates workflows that pause for human approval. The AI generates content, then waits for a human to approve, reject, or modify it.

```
Enter choice: 3
Enter content to generate and review: Write a product description for wireless earbuds

AI Generated Content:
[AI generates a product description]

Enter your review decision:
1. Approve as-is
2. Approve with modifications
3. Reject

Enter decision: 1
Content approved: [approved content is stored]
```

**Workflow code:**
```csharp
builder
    .StartWith(context => ExecutionResult.Next())
    .ChatCompletion(cfg => cfg
        .SystemPrompt("You are a marketing copywriter")
        .UserMessage(data => $"Write about: {data.Topic}")
        .OutputTo(data => data.GeneratedContent))
    .HumanReview(cfg => cfg
        .Content(data => data.GeneratedContent)
        .Reviewer(data => data.Reviewer)
        .OnApproved(data => data.ApprovedContent));
```

## Creating Custom Tools

You can extend the agent's capabilities by creating custom tools:

```csharp
public class StockPriceTool : IAgentTool
{
    public string Name => "stock_price";
    
    public string Description => "Get the current stock price for a ticker symbol";
    
    public string ParametersSchema => @"{
        ""type"": ""object"",
        ""properties"": {
            ""ticker"": { 
                ""type"": ""string"", 
                ""description"": ""Stock ticker symbol (e.g., MSFT, AAPL)"" 
            }
        },
        ""required"": [""ticker""]
    }";

    private readonly IStockService _stockService;

    public StockPriceTool(IStockService stockService)
    {
        _stockService = stockService;
    }

    public async Task<ToolResult> ExecuteAsync(
        string toolCallId, 
        string arguments, 
        CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<StockArgs>(arguments);
        var price = await _stockService.GetPriceAsync(args.Ticker, ct);
        
        return ToolResult.Succeeded(toolCallId, Name, 
            JsonSerializer.Serialize(new { ticker = args.Ticker, price = price }));
    }
}
```

Register your tool:
```csharp
services.AddSingleton<StockPriceTool>();
toolRegistry.Register(serviceProvider.GetRequiredService<StockPriceTool>());
```

## Project Structure

```
WorkflowCore.Sample.AzureFoundry/
├── Program.cs                      # Entry point and service configuration
├── README.md                       # This file
├── .env.example                    # Environment variable template
├── Workflows/
│   ├── WorkflowData.cs            # Data classes for all workflows
│   ├── SimpleChatWorkflow.cs      # Simple LLM chat workflow
│   ├── AgentWithToolsWorkflow.cs  # Agentic workflow with tool calling
│   └── HumanReviewWorkflow.cs     # Human-in-the-loop workflow
└── Tools/
    ├── WeatherTool.cs             # Simulated weather API tool
    └── CalculatorTool.cs          # Mathematical calculator tool
```

## Troubleshooting

### "Resource not found" error

Make sure your endpoint is correct:
- Azure AI Foundry: `https://your-resource.services.ai.azure.com`
- The model name should match a deployed model in your resource

### Authentication errors

1. Verify your API key is correct
2. Make sure the key has access to the resource
3. Check that the model is deployed and accessible

### Tool not being called

The LLM decides when to use tools based on your request. Try being more specific:
- ❌ "calculator" (too vague)
- ✅ "What is 25 + 15?" (clearly needs calculation)

## Learn More

- [WorkflowCore Documentation](https://workflow-core.readthedocs.io)
- [WorkflowCore.AI.AzureFoundry Extension](../../extensions/WorkflowCore.AI.AzureFoundry/)
- [Azure AI Foundry Documentation](https://learn.microsoft.com/azure/ai-services/)
