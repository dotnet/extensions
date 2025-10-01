// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
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
        var endpoint = new Uri(new Uri(Settings.Current.Endpoint), "/openai/v1");
        var credential = new ChainedTokenCredential(new AzureCliCredential(), new DefaultAzureCredential());

        OpenAIClient client;
        if (OfflineOnly)
        {
            var options = new OpenAIClientOptions { Endpoint = endpoint };
            client = new OpenAIClient(new ApiKeyCredential("Bogus"), options);
        }
        else
        {
            // Get Azure token and use as API key
            var token = credential.GetToken(
                new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]),
                default);
            var options = new OpenAIClientOptions { Endpoint = endpoint };
            client = new OpenAIClient(new ApiKeyCredential(token.Token), options);
        }

        return client.GetChatClient(Settings.Current.DeploymentName);
    }
}
