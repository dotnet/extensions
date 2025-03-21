# AI Chat with Custom Data

This project is an AI chat application that demonstrates how to chat with custom data using an AI language model. Please note that this template is currently in an early preview stage. If you have feedback, please take a [brief survey](https://aka.ms/dotnet-chat-template-survey).

>[!NOTE]
> Before running this project you need to configure the API keys or endpoints for the providers you have chosen. See below for details specific to your choices.

# Configure the AI Model Provider
## Setting up a local environment using Ollama
This project is configured to use Ollama, an application that allows you to run AI models locally on your workstation. Note: Ollama is an excellent open source product, but it is not maintained by Microsoft.

### 1. Install Ollama
First, download and install Ollama from their [official website](https://www.ollama.com). Follow the installation instructions specific to your operating system.

### 2. Choose and Install Models
This project uses the `llama3.2` and `all-minilm` language models. To install these models, use the following commands in your terminal once Ollama has been installed:

```sh
ollama pull llama3.2
ollama pull all-minilm
```

### 3. Learn more about Ollama
Once the models are installed, you can start using them in your application. Refer to the [Ollama documentation](https://github.com/ollama/ollama/blob/main/docs/README.md) for detailed instructions on how to explore models locally.

