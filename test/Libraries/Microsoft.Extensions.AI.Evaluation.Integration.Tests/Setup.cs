// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.ML.Tokenizers;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

internal static class Setup
{
    private static bool OfflineOnly =>
        Environment.GetEnvironmentVariable("AITESTING_OFFLINE") == "1";

    internal static ChatConfiguration CreateChatConfiguration()
    {
        var endpoint = new Uri(Settings.Current.Endpoint);
        AzureOpenAIClientOptions options = new AzureOpenAIClientOptions();
        AzureOpenAIClient azureClient =
            OfflineOnly
                ? new AzureOpenAIClient(endpoint, new ApiKeyCredential("Bogus"), options)
                : new AzureOpenAIClient(endpoint, new DefaultAzureCredential(), options);

        IChatClient chatClient = azureClient.AsChatClient(Settings.Current.DeploymentName);
        Tokenizer tokenizer = TiktokenTokenizer.CreateForModel(Settings.Current.ModelName);
        IEvaluationTokenCounter tokenCounter = tokenizer.ToTokenCounter(inputTokenLimit: 6000);
        return new ChatConfiguration(chatClient, tokenCounter);
    }
}
