# Microsoft.Extensions.AI

Provides utilities for working with generative AI components.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.AI
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Examples

Please refer to the [README](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions/#readme-body-tab) for the [Microsoft.Extensions.AI.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions) package.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).

## Running the integration tests

If you're working on this repo and want to run the integration tests, e.g., those in `Microsoft.Extensions.AI.OpenAI.Tests`, you must first set endpoints and keys. You can either set these as environment variables or - better - using .NET's user secrets feature as shown below.

### Configuring OpenAI tests (OpenAI)

Run commands like the following. The settings will be saved in your user profile.

```
cd test/Libraries/Microsoft.Extensions.AI.Integration.Tests
dotnet user-secrets set OpenAI:Mode OpenAI
dotnet user-secrets set OpenAI:Key abcdefghijkl
```

Optionally also run the following. The values shown here are the defaults if you don't specify otherwise:

```
dotnet user-secrets set OpenAI:ChatModel gpt-4o-mini
dotnet user-secrets set OpenAI:EmbeddingModel text-embedding-3-small
```

### Configuring OpenAI tests (Azure OpenAI)

Run commands like the following. The settings will be saved in your user profile.

```
cd test/Libraries/Microsoft.Extensions.AI.Integration.Tests
dotnet user-secrets set OpenAI:Mode AzureOpenAI
dotnet user-secrets set OpenAI:Endpoint https://YOUR_DEPLOYMENT.openai.azure.com/
dotnet user-secrets set OpenAI:Key abcdefghijkl
```

Optionally also run the following. The values shown here are the defaults if you don't specify otherwise:

```
dotnet user-secrets set OpenAI:ChatModel gpt-4o-mini
dotnet user-secrets set OpenAI:EmbeddingModel text-embedding-3-small
```

Your account must have models matching these names.

### Configuring Azure AI Inference tests

Run commands like the following. The settings will be saved in your user profile.

```
cd test/Libraries/Microsoft.Extensions.AI.Integration.Tests
dotnet user-secrets set AzureAIInference:Endpoint https://YOUR_DEPLOYMENT.azure.com/
dotnet user-secrets set AzureAIInference:Key abcdefghijkl
```

Optionally also run the following. The values shown here are the defaults if you don't specify otherwise:

```
dotnet user-secrets set AzureAIInference:ChatModel gpt-4o-mini
dotnet user-secrets set AzureAIInference:EmbeddingModel text-embedding-3-small
```

### Configuring Ollama tests

Run commands like the following. The settings will be saved in your user profile.

```
cd test/Libraries/Microsoft.Extensions.AI.Integration.Tests
dotnet user-secrets set Ollama:Endpoint http://localhost:11434/
```
