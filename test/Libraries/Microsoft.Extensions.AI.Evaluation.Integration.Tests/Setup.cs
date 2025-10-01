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

        OpenAIClient client = OfflineOnly
            ? new OpenAIClient(endpoint, new ApiKeyCredential("Bogus"))
            : new OpenAIClient(endpoint, new AzureTokenCredentialWrapper(credential));

        return client.GetChatClient(Settings.Current.DeploymentName);
    }

    /// <summary>Wraps an Azure TokenCredential for use with OpenAI client.</summary>
    private sealed class AzureTokenCredentialWrapper : ApiKeyCredential
    {
        private readonly TokenCredential _tokenCredential;

        public AzureTokenCredentialWrapper(TokenCredential tokenCredential)
            : base("placeholder")
        {
            _tokenCredential = tokenCredential;
        }

        public override void Deconstruct(out string key)
        {
            // Get Azure token and use it as the API key
            var token = _tokenCredential.GetToken(
                new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]),
                default);
            key = token.Token;
        }
    }
}
