# WorkflowCore.AI.AzureFoundry

[![NuGet](https://img.shields.io/nuget/v/WorkflowCore.AI.AzureFoundry.svg)](https://www.nuget.org/packages/WorkflowCore.AI.AzureFoundry/)

Azure AI Foundry extension for [WorkflowCore](https://github.com/danielgerlag/workflow-core) - enables building AI-powered, agentic workflows with LLM invocation, automatic tool execution, embeddings, RAG search, and human-in-the-loop review patterns.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Available Steps](#available-steps)
  - [ChatCompletion](#chatcompletion)
  - [AgentLoop](#agentloop)
  - [ExecuteTool](#executetool)
  - [GenerateEmbedding](#generateembedding)
  - [VectorSearch](#vectorsearch)
  - [HumanReview](#humanreview)
- [Creating Custom Tools](#creating-custom-tools)
- [Conversation History](#conversation-history)
- [Authentication](#authentication)
- [Samples](#samples)
- [API Reference](#api-reference)

## Features

- **LLM Chat Completion** - Invoke Azure AI models with full conversation history support
- **Agentic Workflows** - Automatic tool-calling loops where the LLM decides which tools to use
- **Tool Execution Framework** - Define and register custom tools that the LLM can invoke
- **Embeddings Generation** - Generate vector embeddings for semantic search and RAG
- **Vector Search** - Integrate with Azure AI Search for similarity search
- **Human-in-the-Loop** - Pause workflows for human review/approval of AI outputs
- **Conversation Persistence** - Automatic conversation history management across workflow steps

## Installation

```bash
dotnet add package WorkflowCore.AI.AzureFoundry
```

## Quick Start

```csharp
// 1. Configure services
services.AddWorkflow();
services.AddAzureFoundry(options =>
{
    options.Endpoint = "https://myresource.services.ai.azure.com";
    options.ApiKey = Environment.GetEnvironmentVariable("AZURE_AI_API_KEY");
    options.DefaultModel = "gpt-4o";
});

// 2. Define a workflow with AI steps
public class CustomerSupportWorkflow : IWorkflow<SupportData>
{
    public string Id => "CustomerSupport";
    public int Version => 1;

    public void Build(IWorkflowBuilder<SupportData> builder)
    {
        builder
            .StartWith(context => ExecutionResult.Next())
            .AgentLoop(cfg => cfg
                .SystemPrompt("You are a helpful customer support agent.")
                .Message(data => data.CustomerQuery)
                .WithTool<SearchKnowledgeBase>()
                .WithTool<CreateTicket>()
                .MaxIterations(5)
                .OutputTo(data => data.Response));
    }
}

// 3. Run the workflow
var workflowId = await host.StartWorkflow("CustomerSupport", new SupportData 
{ 
    CustomerQuery = "How do I reset my password?" 
});
```

## Configuration

### Basic Configuration

```csharp
services.AddAzureFoundry(options =>
{
    // Required: Azure AI Foundry endpoint
    options.Endpoint = "https://myresource.services.ai.azure.com";
    
    // Authentication (choose one)
    options.ApiKey = "your-api-key";  // API key authentication
    // OR
    options.Credential = new DefaultAzureCredential();  // Azure AD authentication
    
    // Model configuration
    options.DefaultModel = "gpt-4o";
    options.DefaultEmbeddingModel = "text-embedding-3-small";
    options.DefaultTemperature = 0.7f;
    options.DefaultMaxTokens = 4096;
    
    // Azure AI Search (optional, for RAG)
    options.SearchEndpoint = "https://mysearch.search.windows.net";
    options.SearchApiKey = "your-search-api-key";
});
```

### Environment Variables

The sample project supports `.env` files:

```bash
AZURE_AI_ENDPOINT=https://myresource.services.ai.azure.com
AZURE_AI_API_KEY=your-api-key
AZURE_AI_DEFAULT_MODEL=gpt-4o
AZURE_AI_PROJECT=myproject
```

## Available Steps

### ChatCompletion

Simple LLM chat completion with optional conversation history.

```csharp
builder
    .ChatCompletion(cfg => cfg
        .SystemPrompt("You are a helpful assistant")
        .UserMessage(data => data.UserQuery)
        .Model("gpt-4o")                    // Optional: override default model
        .Temperature(0.7f)                   // Optional: creativity level (0-1)
        .MaxTokens(1000)                     // Optional: response length limit
        .WithHistory()                       // Optional: enable conversation history
        .OutputTo(data => data.Response)
        .OutputTokensTo(data => data.TokensUsed));
```

**Inputs:**
| Property | Type | Description |
|----------|------|-------------|
| `SystemPrompt` | string | System message defining assistant behavior |
| `UserMessage` | string | User's message/query |
| `Model` | string | Model to use (optional) |
| `Temperature` | float? | Creativity level 0-1 (optional) |
| `MaxTokens` | int? | Maximum response tokens (optional) |

**Outputs:**
| Property | Type | Description |
|----------|------|-------------|
| `Response` | string | LLM's response text |
| `TokensUsed` | int | Total tokens consumed |
| `FinishReason` | string | Why generation stopped |

---

### AgentLoop

Agentic workflow with automatic tool execution. The LLM decides which tools to call, the step executes them, and continues until the LLM provides a final response.

```csharp
builder
    .AgentLoop(cfg => cfg
        .SystemPrompt("You are an agent with access to tools")
        .Message(data => data.UserRequest)
        .WithTool<WeatherTool>()             // Register available tools
        .WithTool<CalculatorTool>()
        .MaxIterations(10)                   // Prevent infinite loops
        .AutoExecuteTools()                  // Automatically execute tool calls
        .OutputTo(data => data.AgentResponse)
        .OutputIterationsTo(data => data.IterationsUsed)
        .OutputToolResultsTo(data => data.ToolResults));
```

**Inputs:**
| Property | Type | Description |
|----------|------|-------------|
| `SystemPrompt` | string | Agent behavior definition |
| `UserMessage` | string | User's request |
| `MaxIterations` | int | Maximum LLM calls (default: 10) |
| `AutomaticMode` | bool | Auto-execute tools (default: true) |
| `AvailableTools` | IList<string> | Tool names to use (empty = all) |

**Outputs:**
| Property | Type | Description |
|----------|------|-------------|
| `Response` | string | Final agent response |
| `IterationsExecuted` | int | Number of LLM calls made |
| `ToolResults` | IList<ToolResult> | Results from tool executions |
| `CompletedSuccessfully` | bool | True if completed before max iterations |

---

### ExecuteTool

Manually execute a specific tool (useful for non-automatic tool orchestration).

```csharp
builder
    .ExecuteTool(cfg => cfg
        .Input(s => s.ToolName, data => "weather")
        .Input(s => s.Arguments, data => JsonSerializer.Serialize(new { city = data.City }))
        .Output(s => s.Result, data => data.ToolOutput));
```

---

### GenerateEmbedding

Generate vector embeddings for semantic similarity and RAG applications.

```csharp
builder
    .GenerateEmbedding(cfg => cfg
        .Input(s => s.Text, data => data.ContentToEmbed)
        .Model("text-embedding-3-small")     // Optional: override model
        .Output(s => s.Embedding, data => data.EmbeddingVector)
        .Output(s => s.TokensUsed, data => data.EmbeddingTokens));
```

**Inputs:**
| Property | Type | Description |
|----------|------|-------------|
| `Text` | string | Text to generate embedding for |
| `Model` | string | Embedding model (optional) |

**Outputs:**
| Property | Type | Description |
|----------|------|-------------|
| `Embedding` | float[] | Vector embedding array |
| `TokensUsed` | int | Tokens consumed |

---

### VectorSearch

Search using vector similarity with Azure AI Search.

```csharp
builder
    .VectorSearch(cfg => cfg
        .Input(s => s.Query, data => data.SearchQuery)
        .Input(s => s.IndexName, data => "knowledge-base")
        .Input(s => s.TopK, data => 5)
        .Input(s => s.Filter, data => "category eq 'support'")  // OData filter
        .Output(s => s.Results, data => data.SearchResults));
```

**Inputs:**
| Property | Type | Description |
|----------|------|-------------|
| `Query` | string | Search query text |
| `IndexName` | string | Azure AI Search index name |
| `TopK` | int | Number of results to return |
| `Filter` | string | OData filter expression (optional) |

**Outputs:**
| Property | Type | Description |
|----------|------|-------------|
| `Results` | IList<SearchResult> | Matching documents with scores |

---

### HumanReview

Pause workflow for human review, approval, or modification of AI-generated content.

```csharp
builder
    .HumanReview(cfg => cfg
        .Content(data => data.AIGeneratedContent)
        .Reviewer(data => data.AssignedReviewer)
        .Prompt("Please review this AI-generated response before sending to customer")
        .CorrelationId(data => data.TicketId)     // Optional: custom event key
        .OnEventKey(data => data.ReviewEventKey)  // Optional: capture the event key
        .OnApproved(data => data.ApprovedContent)
        .OutputDecisionTo(data => data.ReviewDecision));
```

**Inputs:**
| Property | Type | Description |
|----------|------|-------------|
| `Content` | string | The content to be reviewed |
| `Reviewer` | string | Assigned reviewer identifier |
| `ReviewPrompt` | string | Instructions for the reviewer |
| `CorrelationId` | string | Custom event key (optional, defaults to workflowId) |

**Outputs:**
| Property | Type | Description |
|----------|------|-------------|
| `EventKey` | string | The key to use when completing the review |
| `ApprovedContent` | string | Final approved/modified content |
| `Decision` | ReviewDecision | The reviewer's decision |
| `IsApproved` | bool | Whether content was approved |
| `Comments` | string | Reviewer's comments |

**Getting the Event Key:**

There are three ways to get the event key for completing a review:

1. **Use the workflow ID** (default): If you don't provide a `CorrelationId`, the event key equals the workflow ID
2. **Use a custom correlation ID**: Provide your own ID via `.CorrelationId(data => data.MyId)`
3. **Capture the event key**: Use `.OnEventKey(data => data.ReviewEventKey)` to store it in workflow data

**Complete a review by publishing an event:**

```csharp
// Option 1: Use workflow ID (when no CorrelationId was set)
await workflowHost.PublishEvent("HumanReview", workflowId, reviewAction);

// Option 2: Use your custom correlation ID
await workflowHost.PublishEvent("HumanReview", "TICKET-12345", reviewAction);

// Option 3: Use the captured event key from workflow data
await workflowHost.PublishEvent("HumanReview", data.ReviewEventKey, reviewAction);
```

```csharp
var reviewAction = new ReviewAction
{
    Decision = ReviewDecision.Approved,  // or Rejected, ApprovedWithChanges
    Reviewer = "john.doe@example.com",
    ModifiedContent = "Updated content...",  // if modified
    Comments = "Looks good!"
};
```

## Creating Custom Tools

Tools allow the LLM to take actions in your system. Implement `IAgentTool`:

```csharp
public class WeatherTool : IAgentTool
{
    public string Name => "weather";
    
    public string Description => "Get current weather for a city";
    
    public string ParametersSchema => @"{
        ""type"": ""object"",
        ""properties"": {
            ""city"": { 
                ""type"": ""string"", 
                ""description"": ""City name"" 
            }
        },
        ""required"": [""city""]
    }";

    private readonly IWeatherService _weatherService;

    public WeatherTool(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    public async Task<ToolResult> ExecuteAsync(
        string toolCallId, 
        string arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            var args = JsonSerializer.Deserialize<WeatherArgs>(arguments);
            var weather = await _weatherService.GetWeatherAsync(args.City, cancellationToken);
            
            return ToolResult.Succeeded(toolCallId, Name, JsonSerializer.Serialize(weather));
        }
        catch (Exception ex)
        {
            return ToolResult.Failed(toolCallId, Name, ex.Message);
        }
    }
}
```

**Register tools in DI:**

```csharp
// Register tool class
services.AddSingleton<WeatherTool>();
services.AddSingleton<CalculatorTool>();

// Register with tool registry
var toolRegistry = serviceProvider.GetRequiredService<IToolRegistry>();
toolRegistry.Register(serviceProvider.GetRequiredService<WeatherTool>());
toolRegistry.Register(serviceProvider.GetRequiredService<CalculatorTool>());
```

## Conversation History

Conversation history is automatically managed per workflow execution using `IConversationStore`.

### Default In-Memory Store

```csharp
// Enabled by default - conversations stored in memory
services.AddAzureFoundry(options => { ... });
```

### Custom Store Implementation

Implement `IConversationStore` for persistent storage (Redis, SQL, CosmosDB, etc.):

```csharp
public class RedisConversationStore : IConversationStore
{
    public Task<ConversationThread> GetOrCreateThreadAsync(
        string workflowId, string stepId) { ... }
    
    public Task<ConversationThread> GetThreadAsync(string threadId) { ... }
    
    public Task SaveThreadAsync(ConversationThread thread) { ... }
    
    public Task DeleteThreadAsync(string threadId) { ... }
}

// Register custom store
services.AddSingleton<IConversationStore, RedisConversationStore>();
```

## Authentication

### API Key Authentication (Simplest)

```csharp
services.AddAzureFoundry(options =>
{
    options.Endpoint = "https://myresource.services.ai.azure.com";
    options.ApiKey = Environment.GetEnvironmentVariable("AZURE_AI_API_KEY");
});
```

### Azure AD Authentication

```csharp
services.AddAzureFoundry(options =>
{
    options.Endpoint = "https://myresource.services.ai.azure.com";
    options.Credential = new DefaultAzureCredential();
    
    // Or specific credential types:
    // options.Credential = new ManagedIdentityCredential();
    // options.Credential = new ClientSecretCredential(tenantId, clientId, secret);
});
```

## Samples

See the [sample project](../../samples/WorkflowCore.Sample.AzureFoundry/) for complete working examples:

| Sample | Description |
|--------|-------------|
| **Simple Chat** | Basic LLM chat completion workflow |
| **Agent with Tools** | Agentic workflow with weather and calculator tools |
| **Human Review** | Human-in-the-loop approval workflow |

### Running the Sample

```bash
cd src/samples/WorkflowCore.Sample.AzureFoundry
cp .env.example .env
# Edit .env with your Azure AI credentials
dotnet run
```

## API Reference

### Models

| Class | Description |
|-------|-------------|
| `AzureFoundryOptions` | Configuration options for the extension |
| `ConversationMessage` | A single message in a conversation |
| `ConversationThread` | A conversation thread with message history |
| `ToolDefinition` | Defines a tool's name, description, and parameters |
| `ToolResult` | Result from tool execution |
| `SearchResult` | A single search result with score and content |
| `ReviewAction` | Human review decision and modifications |

### Interfaces

| Interface | Description |
|-----------|-------------|
| `IChatCompletionService` | Service for LLM chat completions |
| `IEmbeddingService` | Service for generating embeddings |
| `ISearchService` | Service for vector search |
| `IAgentTool` | Interface for custom tools |
| `IToolRegistry` | Registry for available tools |
| `IConversationStore` | Storage for conversation history |

### Enums

| Enum | Values |
|------|--------|
| `MessageRole` | System, User, Assistant, Tool |
| `ReviewDecision` | Pending, Approved, Rejected, Modified |

## License

This extension is part of WorkflowCore and is released under the [MIT License](../../LICENSE.md).
