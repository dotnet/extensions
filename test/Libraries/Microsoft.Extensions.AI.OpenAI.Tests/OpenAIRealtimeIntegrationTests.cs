// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.TestUtilities;
using OpenAI.RealtimeConversation;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAIRealtimeIntegrationTests
{
    private RealtimeConversationClient? _conversationClient;

    public OpenAIRealtimeIntegrationTests()
    {
        _conversationClient = CreateConversationClient();
    }

    [Fact]
    public async Task CanPerformFunctionCall()
    {
        SkipIfNotEnabled();

        using var conversation = await _conversationClient.StartConversationSessionAsync();
        Assert.NotNull(conversation);
    }

    [MemberNotNull(nameof(_conversationClient))]
    protected void SkipIfNotEnabled()
    {
        if (_conversationClient is null)
        {
            throw new SkipTestException("Client is not enabled.");
        }
    }

    private static RealtimeConversationClient? CreateConversationClient()
    {
        var realtimeModel = Environment.GetEnvironmentVariable("OPENAI_REALTIME_MODEL");
        if (string.IsNullOrEmpty(realtimeModel))
        {
            return null;
        }

        var openAiClient = (AzureOpenAIClient?)IntegrationTestHelpers.GetOpenAIClient();
        return openAiClient?.GetRealtimeConversationClient(realtimeModel);
    }
}
