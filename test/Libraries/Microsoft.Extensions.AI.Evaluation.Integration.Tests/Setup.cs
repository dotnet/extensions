// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
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
        var openAIOptions = new OpenAIClientOptions { Endpoint = new Uri(endpoint.TrimEnd('/') + "/openai/v1") };

        OpenAIClient openAIClient =
            OfflineOnly
                ? new OpenAIClient(new ApiKeyCredential("Bogus"), openAIOptions)
                : new OpenAIClient(new ApiKeyCredential(credential.GetToken(new Azure.Core.TokenRequestContext(["https://cognitiveservices.azure.com/.default"])).Token), openAIOptions);

        return openAIClient;
    }
}
