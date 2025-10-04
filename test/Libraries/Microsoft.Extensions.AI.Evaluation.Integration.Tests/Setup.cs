// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Core;
using Azure.Identity;
using OpenAI;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

internal static class Setup
{
    private static bool OfflineOnly =>
        Environment.GetEnvironmentVariable("AITESTING_OFFLINE") == "1";

    internal static ChatConfiguration CreateChatConfiguration()
    {
        OpenAI.Chat.ChatClient openAIClient = GetOpenAIClient();
        IChatClient chatClient = openAIClient.AsIChatClient();
        return new ChatConfiguration(chatClient);
    }

    private static OpenAI.Chat.ChatClient GetOpenAIClient()
    {
        // Use Azure endpoint with /openai/v1 suffix
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(new Uri(Settings.Current.Endpoint), "/openai/v1")
        };

        OpenAIClient client = OfflineOnly ?
            new OpenAIClient(new ApiKeyCredential("Bogus"), options) :
            new OpenAIClient(
                new BearerTokenPolicy(
                    new ChainedTokenCredential(new AzureCliCredential(), new DefaultAzureCredential()),
                    "https://ai.azure.com/.default"),
                options);

        return client.GetChatClient(Settings.Current.DeploymentName);
    }
}
