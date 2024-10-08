// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.TestUtilities;

namespace Microsoft.Extensions.AI;

public class AzureAIInferenceChatClientIntegrationTests : ChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient() =>
        IntegrationTestHelpers.GetChatCompletionsClient()
            ?.AsChatClient(Environment.GetEnvironmentVariable("AZURE_AI_INFERENCE_CHAT_MODEL") ?? "gpt-4o-mini");

    public override Task CompleteStreamingAsync_UsageDataAvailable() =>
        throw new SkipTestException("Azure.AI.Inference library doesn't currently surface streaming usage data.");
}
