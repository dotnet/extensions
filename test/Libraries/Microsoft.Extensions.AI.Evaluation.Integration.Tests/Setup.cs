// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.SemanticKernel;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

internal static class Setup
{
    private static bool OfflineOnly =>
        Environment.GetEnvironmentVariable("AITESTING_OFFLINE") == "1";

    internal static ChatConfiguration CreateChatConfiguration()
    {
        AzureOpenAIClient azureOpenAIClient = GetAzureOpenAIClient();
        IChatClient chatClient = azureOpenAIClient.GetChatClient(Settings.Current.DeploymentName).AsIChatClient();
        return new ChatConfiguration(chatClient);
    }

    internal static Kernel CreateKernel()
    {
        AzureOpenAIClient azureOpenAIClient = GetAzureOpenAIClient();

        Kernel kernel =
            Kernel
                .CreateBuilder()
                .AddAzureOpenAIChatClient(
                    deploymentName: Settings.Current.DeploymentName,
                    azureOpenAIClient,
                    modelId: Settings.Current.ModelName)
                .Build();

        return kernel;
    }

    private static AzureOpenAIClient GetAzureOpenAIClient()
    {
        var endpoint = new Uri(Settings.Current.Endpoint);
        AzureOpenAIClientOptions options = new();
        var credential = new ChainedTokenCredential(new AzureCliCredential(), new DefaultAzureCredential());
        AzureOpenAIClient azureClient =
            OfflineOnly
                ? new AzureOpenAIClient(endpoint, new ApiKeyCredential("Bogus"), options)
                : new AzureOpenAIClient(endpoint, credential, options);
        return azureClient;
    }
}
