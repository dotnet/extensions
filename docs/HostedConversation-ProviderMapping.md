# IHostedConversationClient — Provider Mapping Report

## Overview

`IHostedConversationClient` is an abstraction for managing server-side (hosted) conversation state across AI providers. It provides a common interface for creating, retrieving, deleting, and managing messages within persistent conversations, decoupling application code from provider-specific conversation/thread/session APIs. Each provider maps these operations to its native primitives, with escape hatches (`RawRepresentation`, `RawRepresentationFactory`, `AdditionalProperties`) for accessing provider-specific features.

## Interface Operations

| Operation | Description | Return Type |
|-----------|-------------|-------------|
| `CreateAsync` | Creates a new hosted conversation | `HostedConversation` |
| `GetAsync` | Retrieves conversation by ID | `HostedConversation` |
| `DeleteAsync` | Deletes a conversation | `void` (Task) |
| `AddMessagesAsync` | Adds messages to a conversation | `void` (Task) |
| `GetMessagesAsync` | Lists messages in a conversation | `IAsyncEnumerable<ChatMessage>` |

## Provider Mapping

### OpenAI (Implemented)

- Maps to `ConversationClient` in `OpenAI.Conversations` namespace
- Full CRUD support via protocol-level APIs
- `RawRepresentation` set to `ClientResult` objects
- ConversationId integrates with `ChatOptions.ConversationId` for inference via `OpenAIResponsesChatClient`
- Metadata limited to 16 key-value pairs (max 64 char keys, 512 char values)

### Azure AI Foundry

- **Azure Foundry v2** uses the OpenAI Responses API directly, so the OpenAI `IHostedConversationClient` implementation works for Azure Foundry v2 without a separate adapter
- The deprecated v1 Agent Service SDK mapped to Thread/Message APIs (`threads.create()`, `threads.get()`, `threads.delete()`, `messages.create()`, `messages.list()`), but this is no longer the recommended path
- **Gaps**: Agent-specific concepts (Runs, Agents) are not in our abstraction; use `AdditionalProperties` for agent-specific metadata

### AWS Bedrock

- Maps to Session Management APIs
- `CreateAsync` → `CreateSession` with optional encryption/metadata
- `GetAsync` → `GetSession`
- `DeleteAsync` → `DeleteSession`
- `AddMessagesAsync` → `PutInvocationStep` (different item model)
- `GetMessagesAsync` → `GetInvocationSteps` (requires translation)
- **Gaps**: Session status (ACTIVE/EXPIRED/ENDED) not in abstraction; encryption config is provider-specific; use `AdditionalProperties` or `RawRepresentationFactory`

### Google Gemini

- Maps to Interactions API
- `CreateAsync` → `interactions.create()` (creates an interaction, not a "conversation" per se)
- `GetAsync` → `interactions.get()`
- `DeleteAsync` → `interactions.delete()`
- `AddMessagesAsync` → No direct equivalent; use `interactions.create()` with `previous_interaction_id` chain
- `GetMessagesAsync` → `interactions.get().outputs` (retrieves outputs, not full message history)
- **Gaps**: Interactions are individual turns, not conversation containers. AddMessages requires creating new interactions chained via `previous_interaction_id`. Provider adapter would need to manage this mapping.

### Anthropic

- **No native conversation CRUD API** — requires local adapter
- Server-side features that CAN assist:
  - **Prompt Caching** (`cache_control`): Stores KV cache of message prefixes (5min/1hr TTL). Adapter should auto-apply cache breakpoints.
  - **Context Compaction** (beta): Server-side summarization when conversations exceed token threshold
  - **Files API** (beta): Store documents server-side for reference across requests
  - **Containers** (beta): Server-side execution state with reusable IDs
- Implementation approach: `LocalHostedConversationClient<TStore>` using local storage (in-memory, SQLite, Redis) with automatic prompt caching optimization
- **Gaps**: All operations are simulated client-side. No server-side conversation persistence.

### Ollama / Local Models

- **No server-side state** at all
- Implementation: Same local adapter pattern as Anthropic but without prompt caching optimization
- **Gaps**: Same as Anthropic — entirely client-side simulation

## Escape Hatches for Provider-Specific Features

### RawRepresentation

Every `HostedConversation` response carries `RawRepresentation` (the underlying provider object). This gives access to 100% of provider functionality:

```csharp
var conversation = await client.CreateAsync();
var openAIResult = (ClientResult)conversation.RawRepresentation; // Access any OpenAI-specific data
```

### RawRepresentationFactory

`HostedConversationCreationOptions.RawRepresentationFactory` allows passing provider-specific creation options:

```csharp
var options = new HostedConversationCreationOptions
{
    RawRepresentationFactory = client => new ConversationCreationOptions
    {
        // Any provider-specific settings
    }
};
```

### AdditionalProperties

`HostedConversation.AdditionalProperties` and `HostedConversationCreationOptions.AdditionalProperties` carry provider-specific data that doesn't fit the common abstraction.

## Feature Coverage Matrix

| Feature | OpenAI | Azure | Bedrock | Gemini | Anthropic | Ollama |
|---------|--------|-------|---------|--------|-----------|--------|
| Create | ✅ Native | ✅ Native | ✅ Native | ✅ Native | ⚠️ Local | ⚠️ Local |
| Get | ✅ Native | ✅ Native | ✅ Native | ✅ Native | ⚠️ Local | ⚠️ Local |
| Delete | ✅ Native | ✅ Native | ✅ Native | ✅ Native | ⚠️ Local | ⚠️ Local |
| AddMessages | ✅ Native | ✅ Native | ⚠️ Translated | ⚠️ Chained | ⚠️ Local | ⚠️ Local |
| GetMessages | ✅ Native | ✅ Native | ⚠️ Translated | ⚠️ Partial | ⚠️ Local | ⚠️ Local |
| Metadata | ✅ 16 KV | ✅ | ✅ | ✅ | ⚠️ Local | ⚠️ Local |
| RawRepresentation | ✅ ClientResult | ✅ AgentThread | ✅ Session | ✅ Interaction | N/A | N/A |

Legend: ✅ = Direct mapping, ⚠️ = Requires translation/local adapter
