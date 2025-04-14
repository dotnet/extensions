# AI Chat with Custom Data

This project is an AI chat application that demonstrates how to chat with custom data using an AI language model. Please note that this template is currently in an early preview stage. If you have feedback, please take a [brief survey](https://aka.ms/dotnet-chat-templatePreview2-survey).

>[!NOTE]
> Before running this project you need to configure the API keys or endpoints for the providers you have chosen. See below for details specific to your choices.

# Configure the AI Model Provider
To use models hosted by GitHub Models, you will need to create a GitHub personal access token. The token should not have any scopes or permissions. See [Managing your personal access tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens).

From the command line, configure your token for this project using .NET User Secrets by running the following commands:

```sh
cd <<your-project-directory>>
dotnet user-secrets set GitHubModels:Token YOUR-TOKEN
```

Learn more about [prototyping with AI models using GitHub Models](https://docs.github.com/github-models/prototyping-with-ai-models).

