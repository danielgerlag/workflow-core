# Changelog

All notable changes to WorkflowCore.AI.AzureFoundry will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-beta.1] - 2026-01-27

### Added

- **ChatCompletion Step** - Invoke Azure AI models with conversation history support
  - Configurable system prompts, temperature, and max tokens
  - Automatic conversation history management
  - Token usage tracking for cost monitoring

- **AgentLoop Step** - Agentic workflows with automatic tool execution
  - LLM-driven tool selection and invocation
  - Configurable iteration limits to prevent runaway loops
  - Support for both automatic and manual tool execution modes
  - Tool result tracking and debugging

- **ExecuteTool Step** - Manual tool execution for fine-grained control
  - Direct tool invocation by name with JSON arguments
  - Error handling with success/failure results

- **GenerateEmbedding Step** - Vector embedding generation
  - Support for Azure AI embedding models
  - Configurable model selection
  - Token usage tracking

- **VectorSearch Step** - Semantic search with Azure AI Search
  - Vector similarity search
  - OData filter support
  - Configurable result count (TopK)

- **HumanReview Step** - Human-in-the-loop approval workflows
  - Pause workflow for human review
  - Support for approve, reject, and modify actions
  - Configurable reviewer assignment and prompts

- **Tool Framework**
  - `IAgentTool` interface for custom tool implementations
  - `IToolRegistry` for tool registration and discovery
  - JSON Schema parameter definitions for tool calling
  - `ToolResult` with success/failure states

- **Conversation History Management**
  - `IConversationStore` abstraction for pluggable storage
  - `InMemoryConversationStore` default implementation
  - Automatic thread management per workflow execution
  - `ConversationMessage` and `ConversationThread` models

- **Azure AI Foundry Integration**
  - Support for Azure AI Foundry (`services.ai.azure.com`) endpoints
  - API key and Azure AD authentication
  - Configurable default models and parameters
  - Azure AI Search integration for RAG scenarios

- **Fluent Builder API**
  - `ChatCompletion()` extension method
  - `AgentLoop()` extension method
  - `GenerateEmbedding()` extension method
  - `VectorSearch()` extension method
  - `HumanReview()` extension method

### Dependencies

- Azure.AI.Inference 1.0.0-beta.5
- Azure.AI.Projects 1.0.0-beta.2
- Azure.Identity 1.13.0
- Azure.Search.Documents 11.6.0

### Notes

- This is a beta release - APIs may change before 1.0.0 stable
- Requires .NET Standard 2.0 or higher
- Compatible with WorkflowCore 3.x

---

## [Unreleased]

### Planned Features

- Streaming response support for real-time output
- Structured output with JSON schema validation
- Vision/multimodal input support
- OpenTelemetry tracing integration
- Rate limiting and retry configuration
- Batch embedding generation
- More conversation store implementations (Redis, SQL, CosmosDB)
