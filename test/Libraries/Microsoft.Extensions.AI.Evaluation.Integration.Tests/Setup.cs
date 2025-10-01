// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Identity;
using OpenAI;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

internal static class Setup
{
    private static bool OfflineOnly =>
        Environment.GetEnvironmentVariable("AITESTING_OFFLINE") == "1";

    internal static ChatConfiguration CreateChatConfiguration()
    {
        OpenAIClient openAIClient = GetOpenAIClient();
        IChatClient chatClient = openAIClient.GetChatClient(Settings.Current.DeploymentName).AsIChatClient();
        return new ChatConfiguration(chatClient);
    }

    private static OpenAIClient GetOpenAIClient()
    {
        var endpoint = Settings.Current.Endpoint;
        var credential = new ChainedTokenCredential(new AzureCliCredential(), new DefaultAzureCredential());

        OpenAIClient openAIClient =
            OfflineOnly
                ? new OpenAIClient(
                    new ApiKeyCredential("Bogus"),
                    new OpenAIClientOptions
                    {
                        Endpoint = new Uri(endpoint.TrimEnd('/') + "/openai/v1")
                    })
                :
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                  new OpenAIClient(
                    new BearerTokenPolicy(
                        credential,
                        "https://cognitiveservices.azure.com/.default"),
                    new OpenAIClientOptions
                    {
                        Endpoint = new Uri(endpoint.TrimEnd('/') + "/openai/v1")
                    });
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        return openAIClient;
    }
}
