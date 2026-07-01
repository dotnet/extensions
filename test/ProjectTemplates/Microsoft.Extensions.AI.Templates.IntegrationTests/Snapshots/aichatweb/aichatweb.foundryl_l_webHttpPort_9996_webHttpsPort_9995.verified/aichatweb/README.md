# AI Chat with Custom Data

This project is an AI chat application that demonstrates how to chat with custom data using an AI language model. Please note that this template is currently in an early preview stage. If you have feedback, please take a [brief survey](https://aka.ms/dotnet-chat-templatePreview2-survey).

>[!NOTE]
> Before running this project you need to configure the API keys or endpoints for the providers you have chosen. See below for details specific to your choices.

# Configure the AI Model Provider
## Setting up a local environment using Foundry Local
This project is configured to use Foundry Local, which runs models on your workstation through a local OpenAI-compatible endpoint. It does not need an API key.

### 1. Install Foundry Local
Install Foundry Local for your operating system by following the [Foundry Local documentation](https://learn.microsoft.com/azure/ai-foundry/foundry-local/).

### 2. Run the app
The app starts the Foundry Local service for you. On first run, it downloads the configured models, then loads them into the local service.

The default chat model alias is `qwen3-4b`. The default embedding model alias is `qwen3-embedding-0.6b`.

### 3. Override model aliases or the service URL
You can change the defaults in `appsettings.json`, `appsettings.Development.json`, or user secrets:

```json
{
  "FoundryLocal": {
    "ChatModel": "qwen3-4b",
    "EmbeddingModel": "qwen3-embedding-0.6b",
    "ServiceUrl": "http://127.0.0.1:5273"
  }
}
```

Use `FoundryLocal:ServiceUrl` if another local process already uses the default port.

