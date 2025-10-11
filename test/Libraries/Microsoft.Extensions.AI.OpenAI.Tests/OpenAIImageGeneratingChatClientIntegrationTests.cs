// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>
/// OpenAI-specific integration tests for ImageGeneratingChatClient.
/// Tests the ImageGeneratingChatClient with OpenAI's chat client implementation.
/// </summary>
public class OpenAIImageGeneratingChatClientIntegrationTests : ImageGeneratingChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient() =>
        IntegrationTestHelpers.GetOpenAIClient()
        ?.GetChatClient(TestRunnerConfiguration.Instance["OpenAI:ChatModel"] ?? "gpt-4o-mini").AsIChatClient();
}
