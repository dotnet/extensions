# AI Chat with Custom Data

This project is an AI chat application that demonstrates how to chat with custom data using an AI language model. Please note that this template is currently in an early preview stage. If you have feedback, please take a [brief survey](https://aka.ms/dotnet-chat-templatePreview2-survey).

>[!NOTE]
> Before running this project you need to configure the API keys or endpoints for the providers you have chosen. See below for details specific to your choices.

# Configure the AI Model Provider
## Using OpenAI

To call the OpenAI REST API, you will need an API key. To obtain one, first [create a new OpenAI account](https://platform.openai.com/signup) or [log in](https://platform.openai.com/login). Next, navigate to the API key page and select "Create new secret key", optionally naming the key. Make sure to save your API key somewhere safe and do not share it with anyone.

From the command line, configure your API key for this project using .NET User Secrets by running the following commands:

```sh
cd <<your-project-directory>>
dotnet user-secrets set OpenAI:Key YOUR-API-KEY
```

## Configure Azure AI Search

To use Azure AI Search, you will need an Azure account and an Azure AI Search resource. For detailed instructions, see the [Azure AI Search documentation](https://learn.microsoft.com/azure/search/search-create-service-portal).

### 1. Create an Azure AI Search Resource
Follow the instructions in the [Azure portal](https://portal.azure.com/) to create an Azure AI Search resource. Note that there is a free tier for the service but it is not currently the default setting on the portal.

Note that if you previously used the same Azure AI Search resource with different model using this project name, you may need to delete your `data-aichatweb-ingested` index using the [Azure portal](https://portal.azure.com/) first before continuing; otherwise, data ingestion may fail due to a vector dimension mismatch.

### 2. Configure Azure AI Search for Keyless Authentication
This template is configured to use keyless authentication (also known as Managed Identity, with Entra ID). Before continuing, you'll need to configure your Azure AI Search resource to support this. [Learn more](https://learn.microsoft.com/azure/search/keyless-connections).  After creation, ensure that you have selected Role-Based Access Control (RBAC) under Settings > Keys, as this is not the default. Assign yourself the roles called out for local development. [Learn more](https://learn.microsoft.com/azure/search/keyless-connections#roles-for-local-development).

### 3. Set the Azure AI Search Endpoint for this app
   Configure your Azure AI Search endpoint for this project, using .NET User Secrets:
   1. In the Azure Portal, navigate to your Azure AI Search resource.
   2. Copy the "URL" from the "Overview" section.
   3. From the command line, configure your Azure AI Search endpoint for this project using .NET User Secrets by running the following commands:

      ```sh
      cd <<your-project-directory>>
      dotnet user-secrets set AzureAISearch:Endpoint YOUR-AZURE-AI-SEARCH-ENDPOINT
      ```

Make sure to replace `YOUR-AZURE-AI-SEARCH-ENDPOINT` with your actual Azure AI Search endpoint.


# Running the application

## Using Visual Studio

1. Open the `.csproj` file in Visual Studio.
2. Press `Ctrl+F5` or click the "Start" button in the toolbar to run the project.

## Using Visual Studio Code

1. Open the project folder in Visual Studio Code.
2. Install the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) for Visual Studio Code.
3. Once installed, Open the `Program.cs` file.
4. Run the project by clicking the "Run" button in the Debug view.

# Learn More
To learn more about development with .NET and AI, check out the following links:

* [AI for .NET Developers](https://learn.microsoft.com/dotnet/ai/)
