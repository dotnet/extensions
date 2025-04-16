# AI Chat with Custom Data

This project is an AI chat application that demonstrates how to chat with custom data using an AI language model. Please note that this template is currently in an early preview stage. If you have feedback, please take a [brief survey](https://aka.ms/dotnet-chat-templatePreview2-survey).

>[!NOTE]
> Before running this project you need to configure the API keys or endpoints for the providers you have chosen. See below for details specific to your choices.

#### ---#if (UseAzure)
### Prerequisites
To use Azure OpenAI or Azure AI Search, you need an Azure account. If you don't already have one, [create an Azure account](https://azure.microsoft.com/free/).

#### ---#endif
#### ---#if (UseQdrant)
### Known Issues

#### Errors After Updating to Aspire Version 9.2.0
This project is not currently compatible with Aspire 9.2.0, and all Aspire package versions are set to 9.1.0. Updating [Aspire.Qdrant.Client](https://www.nuget.org/packages/Aspire.Qdrant.Client) to version 9.2.0 causes an incompatibility with [Microsoft.SemanticKernel.Connectors.Qdrant](https://www.nuget.org/packages/Microsoft.SemanticKernel.Connectors.Qdrant) where different versions of [Qdrant.Client](https://www.nuget.org/packages/Qdrant.Client) are required. Attempting to run the project with `Aspire.Qdrant.Client` version 9.2.0 will result in the following exception:

> System.MissingMethodException: Method not found: 'Qdrant.Client.Grpc.Vectors Qdrant.Client.Grpc.ScoredPoint.get_Vectors()'

Once a version of `Microsoft.SemanticKernel.Connectors.Qdrant` is published with a dependency on `Qdrant.Client` version `>= 1.13.0`, the Aspire packages can also be updated to version 9.2.0.

#### ---#endif
# Configure the AI Model Provider

#### ---#if (IsGHModels)
## Using GitHub Models
To use models hosted by GitHub Models, you will need to create a GitHub personal access token. The token should not have any scopes or permissions. See [Managing your personal access tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens).

#### ---#if (hostIdentifier == "vs")
Configure your token for this project using .NET User Secrets:

1. In Visual Studio, right-click on the ChatWithCustomData-CSharp.AppHost project in the Solution Explorer and select "Manage User Secrets".
2. This opens a `secrets.json` file where you can store your API keys without them being tracked in source control. Add the following key and value:

   ```json
   {
     "ConnectionStrings:openai": "Endpoint=https://models.inference.ai.azure.com;Key=YOUR-API-KEY"
   }
   ```
#### ---#else
From the command line, configure your token for this project using .NET User Secrets by running the following commands:

```sh
cd ChatWithCustomData-CSharp.AppHost
dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://models.inference.ai.azure.com;Key=YOUR-API-KEY"
```
#### ---#endif

Learn more about [prototyping with AI models using GitHub Models](https://docs.github.com/github-models/prototyping-with-ai-models).
#### ---#endif
#### ---#if (IsOpenAI)
## Using OpenAI

To call the OpenAI REST API, you will need an API key. To obtain one, first [create a new OpenAI account](https://platform.openai.com/signup) or [log in](https://platform.openai.com/login). Next, navigate to the API key page and select "Create new secret key", optionally naming the key. Make sure to save your API key somewhere safe and do not share it with anyone.

#### ---#if (hostIdentifier == "vs")
Configure your API key for this project, using .NET User Secrets:

1. In Visual Studio, right-click on the ChatWithCustomData-CSharp.AppHost project in the Solution Explorer and select "Manage User Secrets".
2. This will open a secrets.json file where you can store your API key without them being tracked in source control. Add the following key and value to the file:

   ```json
   {
     "ConnectionStrings:openai": "Key=YOUR-API-KEY"
   }
   ```

#### ---#else
From the command line, configure your API key for this project using .NET User Secrets by running the following commands:

```sh
cd ChatWithCustomData-CSharp.AppHost
dotnet user-secrets set ConnectionStrings:openai "Key=YOUR-API-KEY"
```
#### ---#endif
#### ---#endif
#### ---#if (IsOllama)
## Setting up a local environment for Ollama
This project is configured to run Ollama in a Docker container. Docker Desktop must be installed and running for the project to run successfully. An Ollama container will automatically start when running the application.

Download, install, and run Docker Desktop from the [official website](https://www.docker.com/). Follow the installation instructions specific to your operating system.

Note: Ollama and Docker are excellent open source products, but are not maintained by Microsoft.

#### ---#endif
#### ---#if (IsAzureOpenAI)
## Using Azure OpenAI

To use Azure OpenAI, you will need an Azure account and an Azure OpenAI Service resource. For detailed instructions, see the [Azure OpenAI Service documentation](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource).

### 1. Create an Azure OpenAI Service Resource
[Create an Azure OpenAI Service resource](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).

### 2. Deploy the Models
Deploy the `gpt-4o-mini` and `text-embedding-3-small` models to your Azure OpenAI Service resource. When creating those deployments, give them the same names as the models (`gpt-4o-mini` and `text-embedding-3-small`). See the Azure OpenAI documentation to learn how to [Deploy a model](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource?pivots=web-portal#deploy-a-model).

### 3. Configure API Key and Endpoint
Configure your Azure OpenAI API key and endpoint for this project, using .NET User Secrets:
   1. In the Azure Portal, navigate to your Azure OpenAI resource.
   2. Copy the "Endpoint" URL and "Key 1" from the "Keys and Endpoint" section.
#### ---#if (hostIdentifier == "vs")
   3. In Visual Studio, right-click on the ChatWithCustomData-CSharp.AppHost project in the Solution Explorer and select "Manage User Secrets".
   4. This will open a secrets.json file where you can store your API key and endpoint without it being tracked in source control. Add the following keys & values to the file:

      ```json
      {
        "ConnectionStrings:openai": "Endpoint=https://YOUR-DEPLOYMENT-NAME.openai.azure.com;Key=YOUR-API-KEY"
      }
      ```
#### ---#else
   3. From the command line, configure your API key and endpoint for this project using .NET User Secrets by running the following commands:

      ```sh
      cd ChatWithCustomData-CSharp.AppHost
      dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://YOUR-DEPLOYMENT-NAME.openai.azure.com;Key=YOUR-API-KEY"
      ```
#### ---#endif

Make sure to replace `YOUR-API-KEY` and `YOUR-DEPLOYMENT-NAME` with your actual Azure OpenAI key and endpoint. Make sure your endpoint URL is formatted like https://YOUR-DEPLOYMENT-NAME.openai.azure.com/ (do not include any path after .openai.azure.com/).
#### ---#endif
#### ---#if (UseAzureAISearch)

## Configure Azure AI Search

To use Azure AI Search, you will need an Azure account and an Azure AI Search resource. For detailed instructions, see the [Azure AI Search documentation](https://learn.microsoft.com/azure/search/search-create-service-portal).

### 1. Create an Azure AI Search Resource
Follow the instructions in the [Azure portal](https://portal.azure.com/) to create an Azure AI Search resource. Note that there is a free tier for the service but it is not currently the default setting on the portal.

Note that if you previously used the same Azure AI Search resource with different model using this project name, you may need to delete your `data-ChatWithCustomData-CSharp.Web-ingestion` index using the [Azure portal](https://portal.azure.com/) first before continuing; otherwise, data ingestion may fail due to a vector dimension mismatch.

### 3. Configure API Key and Endpoint
   Configure your Azure AI Search API key and endpoint for this project, using .NET User Secrets:
   1. In the Azure Portal, navigate to your Azure AI Search resource.
   2. Copy the "Endpoint" URL and "Primary admin key" from the "Keys" section.
#### ---#if (hostIdentifier == "vs")
   3. In Visual Studio, right-click on the ChatWithCustomData-CSharp.AppHost project in the Solution Explorer and select "Manage User Secrets".
   4. This will open a `secrets.json` file where you can store your API key and endpoint without them being tracked in source control. Add the following keys and values to the file:

      ```json
      {
        "ConnectionStrings:azureAISearch": "Endpoint=https://YOUR-DEPLOYMENT-NAME.search.windows.net;Key=YOUR-API-KEY"
      }
      ```
#### ---#else
   3. From the command line, configure your API key and endpoint for this project using .NET User Secrets by running the following commands:

      ```sh
      cd ChatWithCustomData-CSharp.AppHost
      dotnet user-secrets set ConnectionStrings:azureAISearch "Endpoint=https://YOUR-DEPLOYMENT-NAME.search.windows.net;Key=YOUR-API-KEY"
      ```
#### ---#endif

Make sure to replace `YOUR-DEPLOYMENT-NAME` and `YOUR-API-KEY` with your actual Azure AI Search deployment name and key.
#### ---#endif
#### ---#if (UseQdrant)

## Setting up a local environment for Qdrant
This project is configured to run Qdrant in a Docker container. Docker Desktop must be installed and running for the project to run successfully. A Qdrant container will automatically start when running the application.

Download, install, and run Docker Desktop from the [official website](https://www.docker.com/). Follow the installation instructions specific to your operating system.

Note: Qdrant and Docker are excellent open source products, but are not maintained by Microsoft.
#### ---#endif

# Running the application

## Using Visual Studio

1. Open the `.sln` file in Visual Studio.
2. Press `Ctrl+F5` or click the "Start" button in the toolbar to run the project.

## Using Visual Studio Code

1. Open the project folder in Visual Studio Code.
2. Install the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) for Visual Studio Code.
3. Once installed, Open the `Program.cs` file in the ChatWithCustomData-CSharp.AppHost project.
4. Run the project by clicking the "Run" button in the Debug view.

## Trust the localhost certificate

Several .NET Aspire templates include ASP.NET Core projects that are configured to use HTTPS by default. If this is the first time you're running the project, an exception might occur when loading the Aspire dashboard. This error can be resolved by trusting the self-signed development certificate with the .NET CLI.

See [Troubleshoot untrusted localhost certificate in .NET Aspire](https://learn.microsoft.com/dotnet/aspire/troubleshooting/untrusted-localhost-certificate) for more information.

# Learn More
To learn more about development with .NET and AI, check out the following links:

* [AI for .NET Developers](https://learn.microsoft.com/dotnet/ai/)
