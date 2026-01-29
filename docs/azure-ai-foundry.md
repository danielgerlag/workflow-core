# Azure AI Foundry Extension

The Azure AI Foundry extension enables building AI-powered, agentic workflows with WorkflowCore. It provides workflow steps for LLM invocation, automatic tool execution, embeddings, vector search, and human-in-the-loop review patterns.

## Installation

```bash
dotnet add package WorkflowCore.AI.AzureFoundry
```

## Overview

This extension adds six new workflow step types:

| Step | Description |
|------|-------------|
| `ChatCompletion` | Invoke LLMs with conversation history |
| `AgentLoop` | Agentic workflows with automatic tool calling |
| `ExecuteTool` | Manual tool execution |
| `GenerateEmbedding` | Create vector embeddings |
| `VectorSearch` | Semantic search with Azure AI Search |
| `HumanReview` | Pause for human approval |

## Configuration

### Basic Setup

```csharp
services.AddWorkflow();

services.AddAzureFoundry(options =>
{
    options.Endpoint = "https://myresource.services.ai.azure.com";
    options.ApiKey = "your-api-key";
    options.DefaultModel = "gpt-4o";
});
```

### Configuration Options

| Option | Type | Description |
|--------|------|-------------|
| `Endpoint` | string | Azure AI Foundry endpoint URL |
| `ApiKey` | string | API key for authentication |
| `Credential` | TokenCredential | Azure AD credential (alternative to ApiKey) |
| `DefaultModel` | string | Default LLM model name |
| `DefaultEmbeddingModel` | string | Default embedding model |
| `DefaultTemperature` | float | Default creativity level (0-1) |
| `DefaultMaxTokens` | int | Default response token limit |
| `SearchEndpoint` | string | Azure AI Search endpoint (optional) |
| `SearchApiKey` | string | Azure AI Search API key (optional) |

## Chat Completion

The simplest way to invoke an LLM in your workflow:

```csharp
public class SimpleChatWorkflow : IWorkflow<ChatData>
{
    public void Build(IWorkflowBuilder<ChatData> builder)
    {
        builder
            .StartWith(context => ExecutionResult.Next())
            .ChatCompletion(cfg => cfg
                .SystemPrompt("You are a helpful assistant")
                .UserMessage(data => data.Question)
                .OutputTo(data => data.Answer));
    }
}
```

### With Conversation History

Enable multi-turn conversations:

```csharp
.ChatCompletion(cfg => cfg
    .SystemPrompt("You are a helpful assistant")
    .UserMessage(data => data.Question)
    .WithHistory()  // Maintains conversation context
    .OutputTo(data => data.Answer));
```

## Agentic Workflows

The `AgentLoop` step enables autonomous AI agents that can use tools to accomplish tasks:

```csharp
public class SupportAgentWorkflow : IWorkflow<SupportData>
{
    public void Build(IWorkflowBuilder<SupportData> builder)
    {
        builder
            .StartWith(context => ExecutionResult.Next())
            .AgentLoop(cfg => cfg
                .SystemPrompt(@"You are a customer support agent.
                    Use the available tools to help customers.
                    Always search the knowledge base before answering.")
                .Message(data => data.CustomerQuery)
                .WithTool<SearchKnowledgeBase>()
                .WithTool<CreateTicket>()
                .WithTool<SendEmail>()
                .MaxIterations(10)
                .OutputTo(data => data.Response));
    }
}
```

### How Agent Loop Works

1. The LLM receives the user message and tool definitions
2. If the LLM decides to use a tool, it returns a tool call request
3. The step executes the tool and feeds the result back to the LLM
4. This continues until the LLM provides a final response (or max iterations)

```
User Message → LLM → Tool Call → Tool Execution → Result → LLM → ... → Final Response
```

## Creating Tools

Tools extend the LLM's capabilities by allowing it to take actions:

```csharp
public class SearchKnowledgeBase : IAgentTool
{
    private readonly IKnowledgeBaseService _kb;

    public SearchKnowledgeBase(IKnowledgeBaseService kb)
    {
        _kb = kb;
    }

    public string Name => "search_knowledge_base";
    
    public string Description => 
        "Search the knowledge base for articles matching the query";
    
    public string ParametersSchema => @"{
        ""type"": ""object"",
        ""properties"": {
            ""query"": { 
                ""type"": ""string"", 
                ""description"": ""Search query"" 
            },
            ""category"": { 
                ""type"": ""string"", 
                ""description"": ""Optional category filter"" 
            }
        },
        ""required"": [""query""]
    }";

    public async Task<ToolResult> ExecuteAsync(
        string toolCallId, 
        string arguments, 
        CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<SearchArgs>(arguments);
        var results = await _kb.SearchAsync(args.Query, args.Category, ct);
        
        if (results.Any())
        {
            return ToolResult.Succeeded(
                toolCallId, 
                Name, 
                JsonSerializer.Serialize(results));
        }
        
        return ToolResult.Succeeded(
            toolCallId, 
            Name, 
            "No articles found matching the query.");
    }
}
```

### Registering Tools

```csharp
// In your DI setup
services.AddSingleton<SearchKnowledgeBase>();
services.AddSingleton<CreateTicket>();

// After building service provider
var toolRegistry = serviceProvider.GetRequiredService<IToolRegistry>();
toolRegistry.Register(serviceProvider.GetRequiredService<SearchKnowledgeBase>());
toolRegistry.Register(serviceProvider.GetRequiredService<CreateTicket>());
```

## Human-in-the-Loop

For workflows requiring human oversight of AI outputs:

```csharp
public class ContentReviewWorkflow : IWorkflow<ContentData>
{
    public void Build(IWorkflowBuilder<ContentData> builder)
    {
        builder
            .StartWith(context => ExecutionResult.Next())
            
            // Generate content with AI
            .ChatCompletion(cfg => cfg
                .SystemPrompt("Generate marketing copy for the product")
                .UserMessage(data => data.ProductDescription)
                .OutputTo(data => data.DraftContent))
            
            // Human reviews before publishing
            .HumanReview(cfg => cfg
                .Content(data => data.DraftContent)
                .Reviewer(data => data.AssignedEditor)
                .Prompt("Review this AI-generated marketing copy")
                .OnApproved(data => data.ApprovedContent)
                .OnDecision(data => data.ReviewDecision))
            
            // Continue based on decision
            .If(data => data.ReviewDecision == ReviewDecision.Approved)
                .Do(then => then
                    .Then<PublishContent>()
                        .Input(step => step.Content, data => data.ApprovedContent));
    }
}
```

### Getting the Event Key

There are two ways to get the event key for completing a review:

**Option 1: Use the workflow ID (simplest)**

By default, if you don't provide a `CorrelationId`, the event key equals the workflow ID:

```csharp
// Start workflow
var workflowId = await host.StartWorkflow("ContentReview", data);

// Later, complete the review using workflowId as the event key
await host.PublishEvent("HumanReview", workflowId, reviewAction);
```

**Option 2: Use a custom correlation ID**

Provide your own correlation ID (e.g., a ticket ID, request ID) for easier integration:

```csharp
// In your workflow
.HumanReview(cfg => cfg
    .Content(data => data.DraftContent)
    .CorrelationId(data => data.TicketId)  // Use your own ID
    .OnApproved(data => data.ApprovedContent))

// Complete the review using your known ID
await host.PublishEvent("HumanReview", "TICKET-12345", reviewAction);
```

**Option 3: Capture the event key in workflow data**

Output the event key to your workflow data for later use:

```csharp
.HumanReview(cfg => cfg
    .Content(data => data.DraftContent)
    .OnEventKey(data => data.ReviewEventKey)  // Capture the key
    .OnApproved(data => data.ApprovedContent))
```

### Completing Reviews

From your UI or API, publish an event to complete the review:

```csharp
await workflowHost.PublishEvent(
    "HumanReview",
    eventKey,  // The workflow ID, custom correlation ID, or captured event key
    new ReviewAction
    {
        Decision = ReviewDecision.Approved,
        Reviewer = "editor@example.com",
        Comments = "Approved with minor edits",
        ModifiedContent = "Updated content..."  // Optional, for modifications
    });
```

## RAG (Retrieval-Augmented Generation)

Combine vector search with LLM generation for knowledge-grounded responses:

```csharp
public class RAGWorkflow : IWorkflow<RAGData>
{
    public void Build(IWorkflowBuilder<RAGData> builder)
    {
        builder
            .StartWith(context => ExecutionResult.Next())
            
            // Search for relevant documents
            .VectorSearch(cfg => cfg
                .Input(s => s.Query, data => data.UserQuestion)
                .Input(s => s.IndexName, data => "company-docs")
                .Input(s => s.TopK, data => 5)
                .Output(s => s.Results, data => data.RelevantDocs))
            
            // Generate answer grounded in documents
            .ChatCompletion(cfg => cfg
                .SystemPrompt(data => $@"Answer based on these documents:
                    {string.Join("\n", data.RelevantDocs.Select(d => d.Content))}
                    If the answer isn't in the documents, say so.")
                .UserMessage(data => data.UserQuestion)
                .OutputTo(data => data.Answer));
    }
}
```

## Embeddings

Generate embeddings for semantic search or similarity:

```csharp
.GenerateEmbedding(cfg => cfg
    .Input(s => s.Text, data => data.Document)
    .Output(s => s.Embedding, data => data.DocumentVector));
```

## Authentication

### API Key (Simplest)

```csharp
options.ApiKey = Environment.GetEnvironmentVariable("AZURE_AI_API_KEY");
```

### Managed Identity (Production)

```csharp
options.Credential = new ManagedIdentityCredential();
```

### Service Principal

```csharp
options.Credential = new ClientSecretCredential(
    tenantId: "your-tenant-id",
    clientId: "your-client-id",
    clientSecret: "your-client-secret"
);
```

## Best Practices

### 1. Set Iteration Limits

Always set `MaxIterations` on `AgentLoop` to prevent runaway costs:

```csharp
.AgentLoop(cfg => cfg
    .MaxIterations(10)  // Stop after 10 LLM calls
    ...);
```

### 2. Write Clear Tool Descriptions

The LLM uses descriptions to decide when to use tools:

```csharp
// ❌ Bad
public string Description => "Gets weather";

// ✅ Good  
public string Description => 
    "Get the current weather conditions for a specific city. " +
    "Returns temperature, humidity, and conditions.";
```

### 3. Use System Prompts Effectively

Guide the agent's behavior with clear instructions:

```csharp
.AgentLoop(cfg => cfg
    .SystemPrompt(@"You are a customer support agent.
        
        Guidelines:
        1. Always be polite and professional
        2. Search the knowledge base before answering
        3. If you can't help, create a support ticket
        4. Never share sensitive customer data")
    ...);
```

### 4. Track Token Usage

Monitor costs by tracking token consumption:

```csharp
.ChatCompletion(cfg => cfg
    ...
    .OutputTokensTo(data => data.TokensUsed));

// In your application
logger.LogInformation("Request used {Tokens} tokens", data.TokensUsed);
```

### 5. Handle Tool Errors Gracefully

Return meaningful error messages from tools:

```csharp
public async Task<ToolResult> ExecuteAsync(...)
{
    try
    {
        var result = await DoWork();
        return ToolResult.Succeeded(id, Name, result);
    }
    catch (NotFoundException)
    {
        return ToolResult.Succeeded(id, Name, 
            "No results found. Try a different search query.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Tool execution failed");
        return ToolResult.Failed(id, Name, 
            "An error occurred. Please try again.");
    }
}
```

## Samples

See the [sample project](https://github.com/danielgerlag/workflow-core/tree/master/src/samples/WorkflowCore.Sample.AzureFoundry) for complete working examples.

## Troubleshooting

### 404 Resource Not Found

Ensure your endpoint ends correctly:
- Azure AI Foundry: `https://resource.services.ai.azure.com`
- The extension automatically appends `/models` to the endpoint

### Authentication Errors

1. Verify your API key or credentials
2. Check that your Azure AD app has the required permissions
3. For managed identity, ensure the identity has access to the AI resource

### Tool Not Being Called

1. Check the tool description is clear about when to use it
2. Verify the tool is registered in the `IToolRegistry`
3. Check the tool's `ParametersSchema` is valid JSON Schema
