# Workflow Core AI (Microsoft.Extensions.AI)

`ChatClientStep` calls a model from a workflow through `IChatClient` from [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai). It works with any provider that has an `IChatClient` (OpenAI, Azure OpenAI, Ollama and others). The core `WorkflowCore` package gets no new dependency.

## Installing

```
PM> Install-Package WorkflowCore.AI
```

## Setup

Register an `IChatClient` and the step:

```c#
services.AddWorkflow();
services.AddChatClient(new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2"));
services.AddWorkflowChatClientStep();
```

## Usage

Inputs:

| Property | Type | |
| --- | --- | --- |
| `Prompt` | `string` | Sent as a user message, after `Messages` |
| `Messages` | `IList<ChatMessage>` | Optional prior conversation |
| `SystemPrompt` | `string` | Optional, sent first |
| `Options` | `ChatOptions` | Optional model id, temperature, max tokens and so on |

Outputs:

| Property | Type | |
| --- | --- | --- |
| `ResponseText` | `string` | The reply text |
| `Response` | `ChatResponse` | The full response, including usage and finish reason |

At least one of `Prompt` or `Messages` is required.

```c#
public class SummaryData
{
    public string Ticket { get; set; }
    public string Summary { get; set; }
}

public class SummarizeTicket : IWorkflow<SummaryData>
{
    public string Id => "summarize-ticket";
    public int Version => 1;

    public void Build(IWorkflowBuilder<SummaryData> builder)
    {
        builder
            .StartWith<ChatClientStep>()
                .Input(step => step.SystemPrompt, data => "Summarize support tickets in one sentence.")
                .Input(step => step.Prompt, data => data.Ticket)
                .Output(data => data.Summary, step => step.ResponseText);
    }
}
```

## Limits

- One request and one response per step. No streaming and no tool calling loop.
- `Response` is assigned to workflow data only if you map it with `.Output(...)`. It is persisted with the workflow data, so prefer `ResponseText` unless you need the extra fields.
- A failing model call throws, so the step follows the workflow's normal error handling and retry settings. A retry sends the request again.
